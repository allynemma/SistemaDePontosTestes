using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaDePontosAPI.Controllers;
using SistemaDePontosAPI.Model;
using SistemaDePontosAPI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class UsersControllerTests
{
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _loggerMock = new Mock<ILogger<UsersController>>();
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_loggerMock.Object, _userServiceMock.Object);
    }

    [Fact]
    public async Task Register_ShouldReturnCreatedAtAction_WhenUserIsValid()
    {
        // Arrange
        var user = new Users { Id = 1, Name = "Test User", Email = "test@example.com", Password = "password", Role = "User" };
        _userServiceMock.Setup(s => s.Register(It.IsAny<Users>())).ReturnsAsync(user);

        // Act
        var result = await _controller.Register(user);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("Get", createdAtActionResult.ActionName);
    }

    [Fact]
    public void Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password";
        var user = new Users { Id = 1, Email = email, Password = password, Name = "Test User", Role = "admin" };
        var token = "testToken";

        _userServiceMock.Setup(s => s.Authenticate(email, password)).Returns(user);
        _userServiceMock.Setup(s => s.GenerateJwtToken(email, user.Id)).Returns(token);

        var context = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        // Act
        var result = _controller.Login(email, password);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        Assert.Equal(token, response.GetType().GetProperty("token")?.GetValue(response, null));
        Assert.Equal(user.Id, response.GetType().GetProperty("Id")?.GetValue(response, null));
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenUserIsFound()
    {
        // Arrange
        var user = new Users { Id = 1, Name = "Test User", Email = "test@example.com", Password = "password", Role = "User" };
        _userServiceMock.Setup(s => s.GetUserById(1)).ReturnsAsync(user);

        // Act
        var result = await _controller.Get(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<Users>(okResult.Value);
        Assert.Equal(user.Id, returnedUser.Id);
    }

    [Fact]
    public async Task Put_ShouldReturnOk_WhenUserIsUpdated()
    {
        // Arrange
        var user = new Users { Id = 1, Name = "Test User", Email = "test@example.com", Password = "password", Role = "User" };
        var updatedUser = new Users { Name = "Updated User", Email = "updated@example.com", Password = "newpassword", Role = "Admin" };
        _userServiceMock.Setup(s => s.UpdateUser(1, updatedUser)).ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.Put(1, updatedUser);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<Users>(okResult.Value);
        Assert.Equal(updatedUser.Name, returnedUser.Name);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenUserIsDeleted()
    {
        // Arrange
        _userServiceMock.Setup(s => s.DeleteUser(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
