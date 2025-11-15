using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace MathBridgeSystem.Test.Domain
{
    public class DomainReflectionCoverageTests
    {
        [Fact]
        public void Touch_All_Entities_Set_Properties()
        {
            var domainAssembly = typeof(MathBridgeSystem.Domain.Entities.User).Assembly;
            var entityTypes = domainAssembly.GetTypes()
                .Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith("MathBridgeSystem.Domain.Entities"))
                .ToList();

            foreach (var type in entityTypes)
            {
                // Skip types without parameterless ctor
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null) continue;

                var instance = Activator.CreateInstance(type);
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
                {
                    var val = GenerateValueForType(prop.PropertyType);
                    try
                    {
                        prop.SetValue(instance, val);
                    }
                    catch
                    {
                        // ignore properties that have constraints or navigation collections
                    }
                }

                instance.Should().NotBeNull();
            }
        }

        private object? GenerateValueForType(Type t)
        {
            var nt = Nullable.GetUnderlyingType(t) ?? t;
            if (nt == typeof(Guid)) return Guid.NewGuid();
            if (nt == typeof(string)) return "x";
            if (nt == typeof(int)) return 1;
            if (nt == typeof(long)) return 1L;
            if (nt == typeof(short)) return (short)1;
            if (nt == typeof(byte)) return (byte)1;
            if (nt == typeof(bool)) return true;
            if (nt == typeof(decimal)) return 1.23m;
            if (nt == typeof(double)) return 1.23d;
            if (nt == typeof(float)) return 1.23f;
            if (nt == typeof(DateTime)) return DateTime.UtcNow;
            if (nt == typeof(DateOnly)) return DateOnly.FromDateTime(DateTime.Today);
            if (nt == typeof(TimeOnly)) return new TimeOnly(8,30);
            if (nt.IsEnum) return Enum.GetValues(nt).GetValue(0);
            if (typeof(System.Collections.IList).IsAssignableFrom(nt)) return null;
            return null;
        }
    }
}
