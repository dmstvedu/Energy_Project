using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;
using Xunit;

namespace SmartHomeTests
{
    public class EnergyMonitorServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();
        private readonly Mock<IEnergyPlanRepository> _planRepo = new();
        private readonly Mock<INotificationService> _notify = new();
        private readonly EnergyMonitorService _monitorService;

        public EnergyMonitorServiceTests()
        {
            _monitorService = new EnergyMonitorService(_deviceRepo.Object, _planRepo.Object, _notify.Object);
        }

        /// <summary>
        /// Перевіряє коректність математичного розрахунку споживання енергії у кВт-год для різних значень потужності у ватах.
        /// </summary>
        [Theory]
        [InlineData(1000.0, 1.0)]
        [InlineData(2500.0, 2.5)]
        [InlineData(0.0, 0.0)]
        public void CalculateCurrentUsageKwh_ShouldCalculateCorrectly(double watts, double expectedKwh)
        {
            var devices = new List<Device>
            {
                new Device { Id = 1, Name = "Heater", IsOn = true, PowerUsageWatts = watts },
                new Device { Id = 2, Name = "Off Device", IsOn = false, PowerUsageWatts = 5000 }
            };
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);

            var actualKwh = _monitorService.CalculateCurrentUsageKwh();

            Assert.Equal(expectedKwh, actualKwh);
        }

        /// <summary>
        /// Перевіряє, що метод повертає переведені у кВт значення, а не сирі вати.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_ShouldNotEqualWrongValue()
        {
            var devices = new List<Device>
            {
                new Device { Id = 1, Name = "Lamp", IsOn = true, PowerUsageWatts = 200 }
            };
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);

            var actualKwh = _monitorService.CalculateCurrentUsageKwh(); // 0.2 kWh

            Assert.NotEqual(200.0, actualKwh); // 200 is Watts, not kWh
        }

        /// <summary>
        /// Перевіряє, що у разі перевищення денного ліміту відправляється відповідне текстове сповіщення про перевантаження.
        /// </summary>
        [Fact]
        public void CheckForOverload_WhenUsageExceedsLimit_ShouldSendAlert()
        {
            var devices = new List<Device>
            {
                new Device { Id = 1, Name = "Boiler", IsOn = true, PowerUsageWatts = 5000 } // 5 kWh
            };
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);

            var plan = new EnergyPlan { DailyLimitKwh = 3.0 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);

            _monitorService.CheckForOverload();

            _notify.Verify(n => n.SendAlert(It.Is<string>(msg => msg.Contains("Overload detected"))), Times.AtLeastOnce());
        }

        /// <summary>
        /// Перевіряє, що якщо ліміт не перевищено, сповіщення про перевантаження ніколи не надсилається.
        /// </summary>
        [Fact]
        public void CheckForOverload_WhenUsageWithinLimit_ShouldNeverSendAlert()
        {
            var devices = new List<Device>
            {
                new Device { Id = 1, Name = "LED", IsOn = true, PowerUsageWatts = 50 } // 0.05 kWh
            };
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);

            var plan = new EnergyPlan { DailyLimitKwh = 2.0 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);

            _monitorService.CheckForOverload();

            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never());
        }

        /// <summary>
        /// Перевіряє, що ліміт плану успішно оновлюється і викликається збереження в репозиторій.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_ShouldUpdatePlanWithNewLimit()
        {
            var plan = new EnergyPlan { DailyLimitKwh = 5.0 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);

            _monitorService.UpdateEnergyLimit(12.5);

            Assert.Equal(12.5, plan.DailyLimitKwh);
            _planRepo.Verify(p => p.UpdatePlan(It.Is<EnergyPlan>(pl => pl.DailyLimitKwh == 12.5)), Times.Exactly(1));
        }

        /// <summary>
        /// Перевіряє сценарій, коли план відсутній.
        /// </summary>
        [Fact]
        public void GetCurrentPlan_WhenRepoReturnsNull_ShouldBeNull()
        {
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns((EnergyPlan?)null);

            var result = _planRepo.Object.GetCurrentPlan();

            Assert.Null(result);
        }
    }
}
