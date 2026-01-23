using Common.Domain.Entities;
using Common.Domain.Interfaces;
using FileIngestor.Application.Features.CargaMasiva.Commands;
using FileIngestor.Application.Features.CargaMasiva.Handlers;
using FileIngestor.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FileIngestor.Application.UnitTests.Features.CargaMasiva
{
    public class UploadFileCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_FileIsUploaded()
        {
            // --- 1. ARRANGE ---
            var cacheMock = new Mock<IDistributedCache>();
            var repoMock = new Mock<IJobStatusRepository>();
            var storageMock = new Mock<IFileUploadService>();
            var publisherMock = new Mock<IMessagePublisher>();

            // Mock de IFormFile
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("archivo_prueba.xlsx");
            mockFile.Setup(f => f.Length).Returns(1024);

            var handler = new UploadFileCommandHandler(
                repoMock.Object,
                storageMock.Object,
                publisherMock.Object,
                cacheMock.Object);

            // Uso de constructor por posición para el record
            var command = new UploadFileCommand(
                mockFile.Object,
                "2026-01",
                "admin@atlantic.com"
            );

            // Mocks de comportamiento
            repoMock.Setup(r => r.GetActiveJobByPeriodAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((CargaArchivo)null);

            storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync("key-123");

            // --- 2. ACT ---
            var result = await handler.Handle(command, CancellationToken.None);

            // --- 3. ASSERT ---
            // FluentAssertions necesita 'using FluentAssertions;' para que .Should() funcione
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // Verificamos que se llamó a Redis para invalidar el historial
            cacheMock.Verify(x => x.RemoveAsync("carga_historial_completo", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}