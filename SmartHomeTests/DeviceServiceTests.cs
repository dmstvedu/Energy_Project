using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;
using Xunit;

namespace SmartHomeTests
{
    public class DeviceServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();
        private readonly DeviceService _deviceService;

        public DeviceServiceTests()
        {
            _deviceService = new DeviceService(_deviceRepo.Object);
        }

        /// <summary>
        /// Перевіряє коректність перемикання стану приладу для різних вхідних значень.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToggleDevice_ShouldUpdateAndReturnNewState(bool newState)
        {
            var device = new Device { Id = 1, Name = "Heater", IsOn = !newState, PowerUsageWatts = 1500 };
            _deviceRepo.Setup(r => r.GetById(1)).Returns(device);

            var result = _deviceService.ToggleDevice(1, newState);

            Assert.Equal(newState, result);
        }

        /// <summary>
        /// Перевіряє, що передача turnOn = true успішно переводить стан приладу в активний.
        /// </summary>
        [Fact]
        public void ToggleDevice_WhenTurnOnIsTrue_ShouldSetIsOnToTrue()
        {
            var device = new Device { Id = 2, Name = "Lamp", IsOn = false };
            _deviceRepo.Setup(r => r.GetById(2)).Returns(device);

            var result = _deviceService.ToggleDevice(2, true);

            Assert.True(result);
            Assert.True(device.IsOn);
        }

        /// <summary>
        /// Перевіряє, що передача turnOn = false коректно вимикає прилад.
        /// </summary>
        [Fact]
        public void ToggleDevice_WhenTurnOnIsFalse_ShouldSetIsOnToFalse()
        {
            var device = new Device { Id = 3, Name = "TV", IsOn = true };
            _deviceRepo.Setup(r => r.GetById(3)).Returns(device);

            var result = _deviceService.ToggleDevice(3, false);

            Assert.False(result);
            Assert.False(device.IsOn);
        }

        /// <summary>
        /// Перевіряє, що спроба перемкнути неіснуючий пристрій викидає ArgumentException.
        /// </summary>
        [Fact]
        public void ToggleDevice_WhenDeviceNotFound_ShouldThrowArgumentException()
        {
            _deviceRepo.Setup(r => r.GetById(99)).Returns((Device?)null);

            var ex = Assert.Throws<ArgumentException>(() => _deviceService.ToggleDevice(99, true));
            Assert.Equal("Device not found", ex.Message);
        }

        /// <summary>
        /// Перевіряє, що метод оновлення репозиторію не викликається, якщо пристрій не було знайдено.
        /// </summary>
        [Fact]
        public void ToggleDevice_WhenDeviceNotFound_ShouldNeverCallUpdate()
        {
            _deviceRepo.Setup(r => r.GetById(404)).Returns((Device?)null);

            Assert.Throws<ArgumentException>(() => _deviceService.ToggleDevice(404, true));
            _deviceRepo.Verify(r => r.Update(It.IsAny<Device>()), Times.Never);
        }

        /// <summary>
        /// Перевіряє, що метод оновлення викликається рівно один раз з коректними параметрами оновленого пристрою.
        /// </summary>
        [Fact]
        public void ToggleDevice_WhenDeviceFound_ShouldCallUpdateWithUpdatedDeviceOnce()
        {
            var device = new Device { Id = 4, Name = "AC", IsOn = false };
            _deviceRepo.Setup(r => r.GetById(4)).Returns(device);

            _deviceService.ToggleDevice(4, true);

            _deviceRepo.Verify(r => r.Update(It.Is<Device>(d => d.Id == 4 && d.IsOn)), Times.Exactly(1));
        }

        /// <summary>
        /// Перевіряє, що метод повертає непорожній список, який містить лише увімкнені прилади.
        /// </summary>
        [Fact]
        public void GetActiveDevices_WhenActiveDevicesExist_ShouldReturnThem()
        {
            var activeDevice = new Device { Id = 1, Name = "Fridge", IsOn = true };
            var inactiveDevice = new Device { Id = 2, Name = "Kettle", IsOn = false };
            var list = new List<Device> { activeDevice, inactiveDevice };

            _deviceRepo.Setup(r => r.GetAll()).Returns(list);

            var activeDevices = _deviceService.GetActiveDevices().ToList();

            Assert.NotNull(activeDevices);
            Assert.NotEmpty(activeDevices);
            Assert.Contains(activeDevice, activeDevices);
        }

        /// <summary>
        /// Перевіряє, що у разі відсутності увімкнених приладів повертається порожня колекція.
        /// </summary>
        [Fact]
        public void GetActiveDevices_WhenAllDevicesAreOff_ShouldReturnEmptyCollection()
        {
            var list = new List<Device>
            {
                new Device { Id = 1, Name = "Router", IsOn = false },
                new Device { Id = 2, Name = "Oven", IsOn = false }
            };
            _deviceRepo.Setup(r => r.GetAll()).Returns(list);

            var activeDevices = _deviceService.GetActiveDevices();

            Assert.Empty(activeDevices);
        }
    }
}
