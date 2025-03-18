using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaDePontosAPI.Controllers;
using SistemaDePontosAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SistemaDePontosAPI;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

public class UsersControllerTests
{
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly Mock<Context> _contextMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _loggerMock = new Mock<ILogger<UsersController>>();
        _contextMock = new Mock<Context>(new DbContextOptions<Context>());
        _configurationMock = new Mock<IConfiguration>();
        _controller = new UsersController(_loggerMock.Object, _contextMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task Register_ShouldReturnCreatedAtAction_WhenUserIsValid()
    {
        // Arrange
        var user = new Users { Id = 1, Name = "Test User", Email = "test@example.com", Password = "password", Role = "User" };
        _contextMock.Setup(c => c.Users.AddAsync(It.IsAny<Users>(), default)).Returns(new ValueTask<EntityEntry<Users>>(Task.FromResult((EntityEntry<Users>)null!)));
        _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.Register(user);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("Get", createdAtActionResult.ActionName);
    }
    public void Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password";
        var nome = "Test User";
        var role = "admin";
        var user = new Users { Id = 1, Email = email, Password = password, Name = nome, Role = role };

        var users = new List<Users> { user }.AsQueryable();

        var mockSet = new Mock<DbSet<Users>>();
        mockSet.As<IQueryable<Users>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Users>(users.Provider));
        mockSet.As<IQueryable<Users>>().Setup(m => m.Expression).Returns(users.Expression);
        mockSet.As<IQueryable<Users>>().Setup(m => m.ElementType).Returns(users.ElementType);
        mockSet.As<IQueryable<Users>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());
        mockSet.As<IAsyncEnumerable<Users>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(new TestAsyncEnumerator<Users>(users.GetEnumerator()));

        _contextMock.Setup(c => c.Users).Returns(mockSet.Object);

        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("testIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("testAudience");

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
        Assert.NotNull(response.GetType().GetProperty("token")?.GetValue(response, null));
        Assert.NotNull(response.GetType().GetProperty("Id")?.GetValue(response, null));
    }


    [Fact]
    public async Task Get_ShouldReturnOk_WhenUserIsFound()
    {
        // Arrange
        var user = new Users { Id = 1, Name = "Test User", Email = "test@example.com", Password = "password", Role = "User" };
        _contextMock.Setup(c => c.Users.FindAsync(1)).ReturnsAsync(user);

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
        _contextMock.Setup(c => c.Users.FindAsync(1)).ReturnsAsync(user);
        _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

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
        var user = new Users { Id = 1, Name = "Test User", Email = "test@example.com", Password = "password", Role = "User" };
        _contextMock.Setup(c => c.Users.FindAsync(1)).ReturnsAsync(user);
        _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
