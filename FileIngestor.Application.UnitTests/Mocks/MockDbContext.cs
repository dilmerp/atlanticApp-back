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

            return mockContext;
        }
    }
}