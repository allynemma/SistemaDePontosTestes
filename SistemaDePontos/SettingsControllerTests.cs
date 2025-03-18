using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaDePontosAPI.Controllers;
using SistemaDePontosAPI.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SistemaDePontosAPI;

public class SettingsControllerTests
{
    private readonly Mock<ILogger<SettingsController>> _loggerMock;
    private readonly Mock<Context> _contextMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _loggerMock = new Mock<ILogger<SettingsController>>();
        _contextMock = new Mock<Context>(new DbContextOptions<Context>());
        _controller = new SettingsController(_loggerMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Post_ShouldReturnCreatedAtAction_WhenSettingsIsValid()
    {
        // Arrange
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.AddAsync(It.IsAny<Settings>(), default)).Returns(new ValueTask<EntityEntry<Settings>>(Task.FromResult((EntityEntry<Settings>)null!)));
        _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.Post(settings);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("Get", createdAtActionResult.ActionName);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenSettingsIsFound()
    {
        // Arrange
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.FindAsync(1)).ReturnsAsync(settings);

        // Act
        var result = await _controller.Get(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSettings = Assert.IsType<Settings>(okResult.Value);
        Assert.Equal(settings.Id, returnedSettings.Id);
    }

    [Fact]
    public async Task Put_ShouldReturnOk_WhenSettingsIsUpdated()
    {
        // Arrange
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        var updatedSettings = new Settings { Workday_Hours = 9, Overtime_Rate = 2.0f };
        _contextMock.Setup(c => c.Settings.FindAsync(1)).ReturnsAsync(settings);
        _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.Put(1, updatedSettings);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSettings = Assert.IsType<Settings>(okResult.Value);
        Assert.Equal(updatedSettings.Workday_Hours, returnedSettings.Workday_Hours);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSettingsIsDeleted()
    {
        // Arrange
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.FindAsync(1)).ReturnsAsync(settings);
        _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
