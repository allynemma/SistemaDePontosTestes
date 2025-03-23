using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaDePontosAPI;
using SistemaDePontosAPI.Controllers;
using SistemaDePontosAPI.Model;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

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
    public void PostSettings_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaims("1", "User");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };

        // Act
        var result = _controller.Post(settings) as UnauthorizedResult;

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void PostSettings_AuthorizedAdmin_ReturnsCreated()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.AddAsync(It.IsAny<Settings>(), default)).Returns(new ValueTask<EntityEntry<Settings>>(Task.FromResult((EntityEntry<Settings>)null!)));
        _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var response = _controller.Post(settings) as CreatedAtActionResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(201, response.StatusCode);
    }

    [Fact]
    public void GetSettings_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaims("1", "User");
        _contextMock.Setup(c => c.Settings.FindAsync(1)).ReturnsAsync(new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f });

        // Act
        var response = _controller.Get(1) as UnauthorizedResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public void GetSettings_AuthorizedAdmin_ReturnsOk()
    {
        // Arrange
        SetUserClaims("1", "admin");
        _contextMock.Setup(c => c.Settings.Find(1)).Returns(new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f });

        // Act
        var response = _controller.Get(1) as OkObjectResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public void PutSettings_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaims("1", "User");
        var settings = new Settings { Workday_Hours = 8, Overtime_Rate = 1.5f };

        // Act
        var response = _controller.Put(1, settings) as UnauthorizedResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public void PutSettings_AuthorizedAdmin_ReturnsOk()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.Find(1)).Returns(settings);
        _contextMock.Setup(c => c.SaveChanges(default)).Returns(1);

        var updatedSettings = new Settings { Workday_Hours = 9, Overtime_Rate = 2.0f };

        // Act
        var response = _controller.Put(1, updatedSettings) as OkObjectResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public void DeleteSettings_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetUserClaims("1", "User");
        _contextMock.Setup(c => c.Settings.Find(1)).Returns(new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f });

        // Act
        var response = _controller.Delete(1) as UnauthorizedResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public void DeleteSettings_AuthorizedAdmin_ReturnsNoContent()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.Find(1)).Returns(settings);
        _contextMock.Setup(c => c.Settings.Remove(settings));
        _contextMock.Setup(c => c.SaveChanges(default)).Returns(1);

        // Act
        var response = _controller.Delete(1) as NoContentResult;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(204, response.StatusCode);
    }

    [Fact]
    public void Post_ShouldReturnCreatedAtAction_WhenSettingsIsValid()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.AddAsync(It.IsAny<Settings>(), default)).Returns(new ValueTask<EntityEntry<Settings>>(Task.FromResult((EntityEntry<Settings>)null!)));
        _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = _controller.Post(settings);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("Get", createdAtActionResult.ActionName);
    }

    [Fact]
    public void Get_ShouldReturnOk_WhenSettingsIsFound()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.Find(1)).Returns(settings);

        // Act
        var result = _controller.Get(1) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void Put_ShouldReturnOk_WhenSettingsIsUpdated()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        var updatedSettings = new Settings { Workday_Hours = 9, Overtime_Rate = 2.0f };
        _contextMock.Setup(c => c.Settings.Find(1)).Returns(settings);
        _contextMock.Setup(c => c.SaveChanges(default)).Returns(1);

        // Act
        var result = _controller.Put(1, updatedSettings) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void Delete_ShouldReturnNoContent_WhenSettingsIsDeleted()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var settings = new Settings { Id = 1, Workday_Hours = 8, Overtime_Rate = 1.5f };
        _contextMock.Setup(c => c.Settings.Find(1)).Returns(settings);
        _contextMock.Setup(c => c.Settings.Remove(settings));
        _contextMock.Setup(c => c.SaveChanges(default)).Returns(1);

        // Act
        var result = _controller.Delete(1) as NoContentResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(204, result.StatusCode);
    }
}

