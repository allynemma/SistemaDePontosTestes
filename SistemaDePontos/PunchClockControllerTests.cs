using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaDePontosAPI;
using SistemaDePontosAPI.Controllers;
using SistemaDePontosAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

public class PunchClockControllerTests
{
    private readonly Mock<ILogger<PunchClockController>> _loggerMock;
    private readonly Mock<Context> _contextMock;
    private readonly PunchClockController _controller;

    public PunchClockControllerTests()
    {
        _loggerMock = new Mock<ILogger<PunchClockController>>();
        _contextMock = new Mock<Context>(new DbContextOptions<Context>());
        _controller = new PunchClockController(_loggerMock.Object, _contextMock.Object);
    }

    private void SetUserClaims(string userId, string role = "user")
    {
        var claims = new List<Claim>
        {
            new Claim("userId", userId),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetAnonymousUser()
    {
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet;
    }

    [Fact]
    public void ResgistroDePonto_ShouldReturnBadRequest_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetAnonymousUser();

        // Act
        var result = _controller.ResgistroDePonto(PunchClockType.CheckIn);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Usuário não autenticado", badRequestResult.Value);
    }

    [Fact]
    public void ResgistroDePonto_ShouldReturnBadRequest_WhenPunchClockTypeIsInvalid()
    {
        // Arrange
        SetUserClaims("1");

        // Act
        var result = _controller.ResgistroDePonto((PunchClockType)999);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Necessário ser check-in ou check-out", badRequestResult.Value);
    }

    [Fact]
    public void ResgistroDePonto_ShouldReturnCreatedAtAction_WhenPunchClockIsValid()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>().AsQueryable();
        var mockSet = CreateMockDbSet(punchClocks);

        _contextMock.Setup(c => c.PunchClocks).Returns(mockSet.Object);
        _contextMock.Setup(c => c.PunchClocks.Add(It.IsAny<PunchClock>())).Verifiable();
        _contextMock.Setup(c => c.SaveChanges()).Returns(1);

        // Act
        var result = _controller.ResgistroDePonto(PunchClockType.CheckIn);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("Historico", createdAtActionResult.ActionName);
    }

    [Fact]
    public void Historico_ShouldReturnBadRequest_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetAnonymousUser();

        // Act
        var result = _controller.Historico(null, null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Usuário não autenticado", badRequestResult.Value);
    }

    [Fact]
    public void Historico_ShouldReturnOk_WhenPunchClocksAreFound()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(punchClocks);
        _contextMock.Setup(c => c.PunchClocks).Returns(mockSet.Object);

        // Act
        var result = _controller.Historico(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void Historico_ShouldReturnBadRequest_WhenStartDateIsGreaterThanEndDate()
    {
        // Arrange
        SetUserClaims("1");

        // Act
        var result = _controller.Historico(null, DateTime.Now.Date.AddDays(1), DateTime.Now.Date);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Data de início não pode ser maior que a data final", badRequestResult.Value);
    }

    [Fact]
    public void Historico_ShouldReturnOk_WhenEndDateIsNull()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(punchClocks);
        _contextMock.Setup(c => c.PunchClocks).Returns(mockSet.Object);

        // Act
        var result = _controller.Historico(null, DateTime.Now.Date, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void Historico_ShouldReturnOk_WhenStartDateIsNull()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(punchClocks);
        _contextMock.Setup(c => c.PunchClocks).Returns(mockSet.Object);

        // Act
        var result = _controller.Historico(null, null, DateTime.Now.Date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void Historico_ShouldReturnOk_WhenBothDatesAreNull()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(punchClocks);
        _contextMock.Setup(c => c.PunchClocks).Returns(mockSet.Object);

        // Act
        var result = _controller.Historico(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void Historico_ShouldReturnOk_WhenPunchClocksAreFilteredByDate()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(punchClocks);
        _contextMock.Setup(c => c.PunchClocks).Returns(mockSet.Object);

        // Act
        var result = _controller.Historico(null, DateTime.Now.Date, DateTime.Now.Date.AddDays(1));

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void GerarRelatorio_ShouldReturnBadRequest_WhenDataInicioIsGreaterThanDataFim()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var dataInicio = DateTime.Now;
        var dataFim = DateTime.Now.AddDays(-1);

        // Act
        var result = _controller.GerarRelatorio(dataInicio, dataFim);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Data de início não pode ser maior que a data final", badRequestResult.Value);
    }

    [Fact]
    public void GerarRelatorio_ShouldReturnCsvFile_WhenDataIsValid()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var dataInicio = DateTime.Now.AddDays(-10);
        var dataFim = DateTime.Now;
        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now.AddDays(-5), PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddDays(-5).AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();

        var dbSetMock = CreateMockDbSet(punchClocks);
        _contextMock.Setup(c => c.PunchClocks).Returns(dbSetMock.Object);

        // Act
        var result = _controller.GerarRelatorio(dataInicio, dataFim);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("relatorio_pontos.csv", fileResult.FileDownloadName);
    }

    [Fact]
    public void ListarPontos_ShouldReturnBadRequest_WhenDataInicioIsGreaterThanDataFim()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var dataInicio = DateTime.Now;
        var dataFim = DateTime.Now.AddDays(-1);

        // Act
        var result = _controller.ListarPontos(null, dataInicio, dataFim);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Data de início não pode ser maior do que data fim", badRequestResult.Value);
    }

    [Fact]
    public void ListarPontos_ShouldReturnOk_WhenNoDatesAreProvided()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();

        var users = new List<Users>
        {
            new Users { Id = 1, Name = "John Doe", Email = "john@example.com", Password = "password", Role = "user" }
        }.AsQueryable();

        var punchClockSetMock = CreateMockDbSet(punchClocks);
        var userSetMock = CreateMockDbSet(users);

        _contextMock.Setup(c => c.PunchClocks).Returns(punchClockSetMock.Object);
        _contextMock.Setup(c => c.Users).Returns(userSetMock.Object);

        // Act
        var result = _controller.ListarPontos(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void ListarPontos_ShouldReturnOk_WhenDatesAreProvided()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var dataInicio = DateTime.Now.AddDays(-10);
        var dataFim = DateTime.Now;

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now.AddDays(-5), PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddDays(-5).AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();

        var users = new List<Users>
        {
            new Users { Id = 1, Name = "John Doe", Email = "john@example.com", Password = "password", Role = "user" }
        }.AsQueryable();

        var punchClockSetMock = CreateMockDbSet(punchClocks);
        var userSetMock = CreateMockDbSet(users);

        _contextMock.Setup(c => c.PunchClocks).Returns(punchClockSetMock.Object);
        _contextMock.Setup(c => c.Users).Returns(userSetMock.Object);

        // Act
        var result = _controller.ListarPontos(null, dataInicio, dataFim);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }
}

