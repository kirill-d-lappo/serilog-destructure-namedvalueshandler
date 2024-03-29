﻿using System.Linq;
using FluentAssertions;
using Serilog.Events;
using Xunit;

namespace Serilog.Destructure.NamedValuesHandler.Tests.DestructuringTests
{
    public class ObjectDestructuringTests : AbstractDestructuringTests
    {
        [Theory]
        [AutoMoqData]
        public void TryDestructureObject_HappyPath_PropertiesAreDestructured(DestructibleEntity value)
        {
            // Arrange
            var expectedProperties = value.GetType().GetProperties();
            var policy = ValueFactories.Instance.EmptyPolicy;

            // Act
            var isDestructured = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isDestructured.Should().BeTrue();
            result.Should().NotBeNull();
            result.Should().BeOfType<StructureValue>();

            var structuredResult = (StructureValue)result;

            structuredResult.Properties.Should().NotBeEmpty();
            structuredResult.Properties.Should().HaveSameCount(expectedProperties);

            structuredResult.Properties.Select(p => p.Name)
                .Should()
                .BeEquivalentTo(expectedProperties.Select(p => p.Name));

            structuredResult.Properties.Select(p => p.Value)
                .Should()
                .BeEquivalentTo(expectedProperties.Select(p => new ScalarValue(p.GetValue(value))));
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureObject_ValueIsMasked_MaskedValueIsDestructured(DestructibleEntity value)
        {
            // Arrange
            var maskedKey = nameof(value.Name);
            var maskedValue = value.Name;
            var expectedMaskedValue = new ScalarValue(maskedValue.Mask());

            var policy = new NamedValueHandlersBuilder()
                .Mask(maskedKey)
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (StructureValue)result;
            dictionaryResult.Properties.Should()
                .Contain(e => e.Name == maskedKey && Equals(e.Value, expectedMaskedValue));
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureObject_ValueIsOmitted_ByName_ValueIsRemoved(DestructibleEntity value)
        {
            // Arrange
            var omittedKey = nameof(value.Name);

            var policy = new NamedValueHandlersBuilder()
                .Omit(omittedKey)
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (StructureValue)result;
            dictionaryResult.Properties.Should()
                .NotContain(e => e.Name == omittedKey);
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureObject_ValueIsOmitted_ByType_ValueIsRemoved(DestructibleEntity value)
        {
            // Arrange
            var omittedKey = nameof(value.Name);
            var omittedValue = value.Name;

            var policy = new NamedValueHandlersBuilder()
                .OmitType(omittedValue.GetType())
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (StructureValue)result;
            dictionaryResult.Properties.Should()
                .NotContain(e => e.Name == omittedKey);
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureObject_ValueIsOmitted_ByNamespace_ValueIsRemoved(DestructibleEntity value)
        {
            // Arrange
            var omittedKey = nameof(value.Name);
            var omittedValue = value.Name;
            var omittedNamespace = omittedValue.GetType().Namespace?.Split(".").First();

            var policy = new NamedValueHandlersBuilder()
                .OmitNamespace(omittedNamespace)
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (StructureValue)result;
            dictionaryResult.Properties.Should()
                .NotContain(e => e.Name == omittedKey);
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureObject_ValueIsNotMaskedOrOmitted_ValueIsNotChanged(DestructibleEntity value)
        {
            // Arrange
            var notModifiedKey = nameof(value.Name);
            var notModifiedValue = new ScalarValue(value.Name);

            var policy = new NamedValueHandlersBuilder()
                .Mask($"{notModifiedKey}:masked")
                .OmitNamespace("Special.Namespace", "Legacy")
                .OmitType(typeof(int))
                .Omit($"{notModifiedKey}:omitted")
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (StructureValue)result;
            dictionaryResult.Properties.Should()
                .Contain(e => e.Name == notModifiedKey && Equals(e.Value, notModifiedValue));
        }
    }
}
