using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaDePontosAPI;
using SistemaDePontosAPI.Controllers;
using SistemaDePontosAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    [Fact]
    public void ResgistroDePonto_ShouldReturnBadRequest_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Lista de PunchClocks em memória (vazia para evitar conflitos)
        var punchClocks = new List<PunchClock>().AsQueryable();

        // Mock do DbSet<PunchClock>
        var mockSet = new Mock<DbSet<PunchClock>>();
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Provider).Returns(punchClocks.Provider);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Expression).Returns(punchClocks.Expression);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.ElementType).Returns(punchClocks.ElementType);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.GetEnumerator()).Returns(punchClocks.GetEnumerator());

        // Configurar o contexto mockado
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
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var punchClocks = new List<PunchClock>
    {
        new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
        new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
    }.AsQueryable();

        var mockSet = new Mock<DbSet<PunchClock>>();
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Provider).Returns(punchClocks.Provider);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Expression).Returns(punchClocks.Expression);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.ElementType).Returns(punchClocks.ElementType);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.GetEnumerator()).Returns(punchClocks.GetEnumerator());

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
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        // Act
        var result = _controller.Historico(null, DateTime.Now.Date.AddDays(1), DateTime.Now.Date);
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Data de início não pode ser maior que a data final", badRequestResult.Value);
    }

    [Fact]
    public void Historico_ShouldReturnOk_WhenEndDateIsNull()
    {
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();
        var mockSet = new Mock<DbSet<PunchClock>>();
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Provider).Returns(punchClocks.Provider);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Expression).Returns(punchClocks.Expression);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.ElementType).Returns(punchClocks.ElementType);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.GetEnumerator()).Returns(punchClocks.GetEnumerator());
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
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();
        var mockSet = new Mock<DbSet<PunchClock>>();
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Provider).Returns(punchClocks.Provider);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Expression).Returns(punchClocks.Expression);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.ElementType).Returns(punchClocks.ElementType);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.GetEnumerator()).Returns(punchClocks.GetEnumerator());
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
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        }.AsQueryable();
        var mockSet = new Mock<DbSet<PunchClock>>();
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Provider).Returns(punchClocks.Provider);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Expression).Returns(punchClocks.Expression);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.ElementType).Returns(punchClocks.ElementType);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.GetEnumerator()).Returns(punchClocks.GetEnumerator());
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
        var claims = new List<Claim> { new Claim("userId", "1") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var punchClocks = new List<PunchClock>
    {
        new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
        new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
    }.AsQueryable();
        var mockSet = new Mock<DbSet<PunchClock>>();
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Provider).Returns(punchClocks.Provider);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.Expression).Returns(punchClocks.Expression);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.ElementType).Returns(punchClocks.ElementType);
        mockSet.As<IQueryable<PunchClock>>().Setup(m => m.GetEnumerator()).Returns(punchClocks.GetEnumerator());
        _contextMock.Setup(c => c.PunchClocks).Returns(mockSet.Object);
        // Act
        var result = _controller.Historico(null, DateTime.Now.Date, DateTime.Now.Date.AddDays(1));
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }


}

