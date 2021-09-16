﻿using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Serilog.Destructure.NamedValuesHandler.Tests.DestructuringTests
{
    public class SimpleValueDestructuringTests
    {
        private ILogEventPropertyValueFactory ScalarOnlyFactory { get; } = ValueFactories.Instance.ScalarOnlyFactory;

        [Theory]
        [AutoMoqData]
        public void TryDestructureString_HandleAllStringsShouldBeMasked_StringValueIsMasked(string value)
        {
            // Arrange
            const string Mask = "******";
            var policy = new NamedValueHandlersBuilder()
                .Handle<string>((_, _) => Mask)
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (ScalarValue)result;
            dictionaryResult.Value.Should().Be(Mask);
        }
    }
}
