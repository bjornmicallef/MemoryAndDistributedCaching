using MemoryAndDistributedCaching.Core.Models;
using MemoryAndDistributedCaching.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemoryAndDistrubutedCaching.Tests
{
    [TestClass]
    public class CacheServiceTests
    {
        private CacheService _cacheService;
        private Mock<IConnectionMultiplexer> _mockMuxer;
        private Mock<IDatabase> _mockRedisDb;
        private Mock<IMemoryCache> _mockMemCache;

        public CacheServiceTests()
        {
            _mockMuxer = new Mock<IConnectionMultiplexer>();
            _mockRedisDb = new Mock<IDatabase>();
            _mockMemCache = new Mock<IMemoryCache>();
        }

        #region GetOrSet
        [TestMethod]
        public async Task GetOrSet_KeyFoundInMemoryCache_ReturnsValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            memoryCache.Set(key, value);

            _cacheService = new CacheService(_mockMuxer.Object, memoryCache);

            // Act
            var result = await _cacheService.GetOrSet<string>(key, () => Task.FromResult(value), TimeSpan.FromSeconds(30));

            // Assert
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public async Task GetOrSet_KeyFoundInMemoryCache_ReturnsParsedAndMappedJsonValue()
        {
            // Arrange
            var key = "TestKey";
            var value = GenerateOpenWeather();

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            memoryCache.Set(key, value);

            _cacheService = new CacheService(_mockMuxer.Object, memoryCache);

            // Act
            var result = await _cacheService.GetOrSet<OpenWeather>(key, () => Task.FromResult<OpenWeather>(value), TimeSpan.FromSeconds(30));

            // Assert
            Assert.IsInstanceOfType(result, typeof(OpenWeather));
            Assert.AreEqual(value.main.temp, result.main.temp);
            Assert.AreEqual(value.main.temp_max, result.main.temp_max);
            Assert.AreEqual(value.main.temp_min, result.main.temp_min);
            Assert.AreEqual(value.weather[0].description, result.weather[0].description);
        }

        [TestMethod]
        public async Task GetOrSet_KeyFoundInDistributedCache_ReturnsValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockRedisDb.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>())).ReturnsAsync(value);
            _mockMuxer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockRedisDb.Object);

            _cacheService = new CacheService(_mockMuxer.Object, memoryCache);

            // Act
            var result = await _cacheService.GetOrSet<string>(key, () => Task.FromResult(value), TimeSpan.FromSeconds(30));

            // Assert
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public async Task GetOrSet_KeyFoundInDistributedCache_ReturnsParsedAndMappedJsonValue()
        {
            // Arrange
            var key = "TestKey";
            var value = GenerateOpenWeather();

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockRedisDb.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>())).ReturnsAsync(JsonConvert.SerializeObject(value));
            _mockMuxer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockRedisDb.Object);

            _cacheService = new CacheService(_mockMuxer.Object, memoryCache);

            // Act
            var result = await _cacheService.GetOrSet<OpenWeather>(key, () => Task.FromResult<OpenWeather>(value), TimeSpan.FromSeconds(30));

            // Assert
            Assert.IsInstanceOfType(result, typeof(OpenWeather));
            Assert.AreEqual(value.main.temp, result.main.temp);
            Assert.AreEqual(value.main.temp_max, result.main.temp_max);
            Assert.AreEqual(value.main.temp_min, result.main.temp_min);
            Assert.AreEqual(value.weather[0].description, result.weather[0].description);
        }

        [TestMethod]
        public async Task GetOrSet_KeyNotFoundInEitherCache_ReturnsValueFromFunctionAndSetsCache()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockRedisDb.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>())).ReturnsAsync(string.Empty);
            _mockMuxer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockRedisDb.Object);

            _cacheService = new CacheService(_mockMuxer.Object, memoryCache);

            // Act
            var result = await _cacheService.GetOrSet<string>(key, () => Task.FromResult(value), TimeSpan.FromSeconds(30));

            // Assert
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(value, result);
            _mockRedisDb.Verify(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [TestMethod]
        public async Task GetOrSet_ExceptionThrown_ReturnsDefault()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockRedisDb.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>())).ThrowsAsync(new Exception("Some random Redis cache exception"));
            _mockMuxer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockRedisDb.Object);

            _cacheService = new CacheService(_mockMuxer.Object, memoryCache);

            // Act
            var result = await _cacheService.GetOrSet<string>(key, () => Task.FromResult(value), TimeSpan.FromSeconds(30));

            // Assert
            Assert.IsNull(result);
        }
        #endregion

        #region PrivateMethods
        private OpenWeather GenerateOpenWeather()
        {
            var main = new Main
            {
                temp = 25,
                temp_max = 25,
                temp_min = 25
            };

            var weather = new Weather
            {
                description = "Sunny",
            };

            var listWeather = new List<Weather>();
            listWeather.Add(weather);

            var openWeather = new OpenWeather
            {
                weather = listWeather,
                main = main
            };

            return openWeather;
        }
        #endregion
    }
}