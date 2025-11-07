using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq;

namespace MathBridgeSystem.Tests.Helpers
{
    public static class DbSetExtensions
    {
        public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> data) where T : class
        {
            var mock = new Mock<DbSet<T>>();
            mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            return mock;
        }
    }
}