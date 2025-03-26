using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaDePontosAPI.Controllers;
using SistemaDePontosAPI.Model;
using SistemaDePontosAPI.Services;
using System.Security.Claims;
using SistemaDePontosAPI.Mensageria;
public class PunchClockControllerTests
{
    private readonly Mock<ILogger<PunchClockController>> _loggerMock;
    private readonly Mock<IPunchClockService> _punchClockServiceMock;
    private readonly Mock<KafkaProducer> _kafkaProducerMock;
    private readonly PunchClockController _controller;

    public PunchClockControllerTests()
    {
        _loggerMock = new Mock<ILogger<PunchClockController>>();
        _punchClockServiceMock = new Mock<IPunchClockService>();
        _kafkaProducerMock = new Mock<KafkaProducer>("localhost:9092", "pontos");
        _controller = new PunchClockController(_loggerMock.Object, _punchClockServiceMock.Object, _kafkaProducerMock.Object);
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

    [Fact]
    public async Task ResgistroDePonto_ShouldReturnBadRequest_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetAnonymousUser();

        // Act
        var result = await _controller.ResgistroDePonto(PunchClockType.CheckIn);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Usuário não autenticado", badRequestResult.Value);
    }

    [Fact]
    public async Task ResgistroDePonto_ShouldReturnBadRequest_WhenPunchClockTypeIsInvalid()
    {
        // Arrange
        SetUserClaims("1");

        // Act
        var result = await _controller.ResgistroDePonto((PunchClockType)999);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Necessário ser check-in ou check-out", badRequestResult.Value);
    }

    [Fact]
    public async Task ResgistroDePonto_ShouldReturnCreatedAtAction_WhenPunchClockIsValid()
    {
        // Arrange
        SetUserClaims("1");

        _punchClockServiceMock.Setup(s => s.HasCheckedInToday(It.IsAny<int>())).Returns(false);
        _punchClockServiceMock.Setup(s => s.HasCheckedOutToday(It.IsAny<int>())).Returns(false);
        _punchClockServiceMock.Setup(s => s.RegisterPunchClock(It.IsAny<int>(), It.IsAny<PunchClockType>()))
            .ReturnsAsync(new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn });

        // Act
        var result = await _controller.ResgistroDePonto(PunchClockType.CheckIn);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("Historico", createdAtActionResult.ActionName);
    }

    [Fact]
    public async Task Historico_ShouldReturnBadRequest_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetAnonymousUser();

        // Act
        var result = await _controller.Historico(null, null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Usuário não autenticado", badRequestResult.Value);
    }

    [Fact]
    public async Task Historico_ShouldReturnOk_WhenPunchClocksAreFound()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        };

        _punchClockServiceMock.Setup(s => s.GetPunchClocksByUserId(It.IsAny<int>(), null, null)).ReturnsAsync(punchClocks);

        // Act
        var result = await _controller.Historico(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Historico_ShouldReturnBadRequest_WhenStartDateIsGreaterThanEndDate()
    {
        // Arrange
        SetUserClaims("1");

        // Act
        var result = await _controller.Historico(null, DateTime.Now.Date.AddDays(1), DateTime.Now.Date);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Data de início não pode ser maior que a data final", badRequestResult.Value);
    }

    [Fact]
    public async Task Historico_ShouldReturnOk_WhenEndDateIsNull()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        };

        _punchClockServiceMock.Setup(s => s.GetPunchClocksByUserId(It.IsAny<int>(), It.IsAny<DateTime?>(), null)).ReturnsAsync(punchClocks);

        // Act
        var result = await _controller.Historico(null, DateTime.Now.Date, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Historico_ShouldReturnOk_WhenStartDateIsNull()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        };

        _punchClockServiceMock.Setup(s => s.GetPunchClocksByUserId(It.IsAny<int>(), null, It.IsAny<DateTime?>())).ReturnsAsync(punchClocks);

        // Act
        var result = await _controller.Historico(null, null, DateTime.Now.Date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Historico_ShouldReturnOk_WhenBothDatesAreNull()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        };

        _punchClockServiceMock.Setup(s => s.GetPunchClocksByUserId(It.IsAny<int>(), null, null)).ReturnsAsync(punchClocks);

        // Act
        var result = await _controller.Historico(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Historico_ShouldReturnOk_WhenPunchClocksAreFilteredByDate()
    {
        // Arrange
        SetUserClaims("1");

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        };

        _punchClockServiceMock.Setup(s => s.GetPunchClocksByUserId(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).ReturnsAsync(punchClocks);

        // Act
        var result = await _controller.Historico(null, DateTime.Now.Date, DateTime.Now.Date.AddDays(1));

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GerarRelatorio_ShouldReturnBadRequest_WhenDataInicioIsGreaterThanDataFim()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var dataInicio = DateTime.Now;
        var dataFim = DateTime.Now.AddDays(-1);

        // Act
        var result = await _controller.GerarRelatorio(dataInicio, dataFim);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Data de início não pode ser maior que a data final", badRequestResult.Value);
    }

    [Fact]
    public async Task GerarRelatorio_ShouldReturnCsvFile_WhenDataIsValid()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var dataInicio = DateTime.Now.AddDays(-10);
        var dataFim = DateTime.Now;
        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now.AddDays(-5), PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddDays(-5).AddHours(8), PunchClockType = PunchClockType.CheckOut }
        };

        _punchClockServiceMock.Setup(s => s.GetPunchClocksForReport(dataInicio, dataFim)).ReturnsAsync(punchClocks);

        // Act
        var result = await _controller.GerarRelatorio(dataInicio, dataFim);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("relatorio_pontos.csv", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ListarPontos_ShouldReturnBadRequest_WhenDataInicioIsGreaterThanDataFim()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var dataInicio = DateTime.Now;
        var dataFim = DateTime.Now.AddDays(-1);

        // Act
        var result = await _controller.ListarPontos(null, dataInicio, dataFim);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Data de início não pode ser maior que a data final", badRequestResult.Value);
    }

    [Fact]
    public async Task ListarPontos_ShouldReturnOk_WhenNoDatesAreProvided()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now, PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddHours(8), PunchClockType = PunchClockType.CheckOut }
        };

        _punchClockServiceMock.Setup(s => s.GetAllPunchClocks(null, null, null)).ReturnsAsync(punchClocks);

        // Act
        var result = await _controller.ListarPontos(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task ListarPontos_ShouldReturnOk_WhenDatesAreProvided()
    {
        // Arrange
        SetUserClaims("1", "admin");
        var dataInicio = DateTime.Now.AddDays(-10);
        var dataFim = DateTime.Now;

        var punchClocks = new List<PunchClock>
        {
            new PunchClock { Id = 1, UserId = 1, Timestamp = DateTime.Now.AddDays(-5), PunchClockType = PunchClockType.CheckIn },
            new PunchClock { Id = 2, UserId = 1, Timestamp = DateTime.Now.AddDays(-5).AddHours(8), PunchClockType = PunchClockType.CheckOut }
        };

        _punchClockServiceMock.Setup(s => s.GetAllPunchClocks(null, dataInicio, dataFim)).ReturnsAsync(punchClocks);

        // Act
        var result = await _controller.ListarPontos(null, dataInicio, dataFim);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }
}

