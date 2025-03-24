using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaDePontosAPI.Controllers;
using SistemaDePontosAPI.Model;
using SistemaDePontosAPI.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

public class SettingsControllerTests
{
    private readonly Mock<ILogger<SettingsController>> _loggerMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _loggerMock = new Mock<ILogger<SettingsController>>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _controller = new SettingsController(_loggerMock.Object, _settingsServiceMock.Object);
    }

    private void SetUserClaims(string userId, string role = "User")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Test"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task PostSettings_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaims("1", "User");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };

        // Act
        var result = await _controller.Post(settings) as UnauthorizedResult;

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task PostSettings_AuthorizedAdmin_ReturnsCreated()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _settingsServiceMock.Setup(s => s.CreateSettings(It.IsAny<Settings>())).ReturnsAsync(settings);

        // Act
        var response = await _controller.Post(settings) as CreatedAtActionResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(201, response.StatusCode);
    }

    [Fact]
    public async Task GetSettings_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaims("1", "User");

        // Act
        var response = await _controller.Get(1) as UnauthorizedResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public async Task GetSettings_AuthorizedAdmin_ReturnsOk()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _settingsServiceMock.Setup(s => s.GetSettingsById(1)).ReturnsAsync(settings);

        // Act
        var response = await _controller.Get(1) as OkObjectResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task PutSettings_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaims("1", "User");
        var settings = new Settings { Workday_Hours = 8, Overtime_Rate = 1.5f };

        // Act
        var response = await _controller.Put(1, settings) as UnauthorizedResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public async Task PutSettings_AuthorizedAdmin_ReturnsOk()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        var updatedSettings = new Settings { Workday_Hours = 9, Overtime_Rate = 2.0f };
        _settingsServiceMock.Setup(s => s.UpdateSettings(1, updatedSettings)).ReturnsAsync(updatedSettings);

        // Act
        var response = await _controller.Put(1, updatedSettings) as OkObjectResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSettings_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaims("1", "User");

        // Act
        var response = await _controller.Delete(1) as UnauthorizedResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSettings_AuthorizedAdmin_ReturnsNoContent()
    {
        // Arrange
        SetUserClaims("1", "admin");
        _settingsServiceMock.Setup(s => s.DeleteSettings(1)).ReturnsAsync(true);

        // Act
        var response = await _controller.Delete(1) as NoContentResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(204, response.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldReturnCreatedAtAction_WhenSettingsIsValid()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _settingsServiceMock.Setup(s => s.CreateSettings(It.IsAny<Settings>())).ReturnsAsync(settings);

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
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _settingsServiceMock.Setup(s => s.GetSettingsById(1)).ReturnsAsync(settings);

        // Act
        var result = await _controller.Get(1) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task Put_ShouldReturnOk_WhenSettingsIsUpdated()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        var updatedSettings = new Settings { Workday_Hours = 9, Overtime_Rate = 2.0f };
        _settingsServiceMock.Setup(s => s.UpdateSettings(1, updatedSettings)).ReturnsAsync(updatedSettings);

        // Act
        var result = await _controller.Put(1, updatedSettings) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSettingsIsDeleted()
    {
        // Arrange
        SetUserClaims("1", "admin");
        _settingsServiceMock.Setup(s => s.DeleteSettings(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1) as NoContentResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(204, result.StatusCode);
    }
}
