using Common.Domain.Entities;
using Common.Domain.Interfaces;
using FileIngestor.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FileIngestor.Application.UnitTests.Mocks
{
    public static class MockDbContext
    {
        public static Mock<IApplicationDbContext> GetDbContext()
        {
            var mockContext = new Mock<IApplicationDbContext>();

            // Si necesitas que el Mock devuelva una lista inicial de archivos:
            // var data = new List<CargaArchivo>().AsQueryable();
            // mockContext.Setup(m => m.CargaArchivos).ReturnsDbSet(data);

            return mockContext;
        }
    }
}