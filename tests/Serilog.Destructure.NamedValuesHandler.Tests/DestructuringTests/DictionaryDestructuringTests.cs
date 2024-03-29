﻿using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Serilog.Events;
using Xunit;

namespace Serilog.Destructure.NamedValuesHandler.Tests.DestructuringTests
{
    public class DictionaryDestructuringTests : AbstractDestructuringTests
    {
        [Theory]
        [AutoMoqData]
        public void TryDestructureDictionary_HappyPath_ValueIsDestructed(Dictionary<string, string> value)
        {
            // Arrange
            var policy = ValueFactories.Instance.EmptyPolicy;

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();
            result.Should().BeOfType<DictionaryValue>();

            var dictionaryResult = (DictionaryValue)result;
            dictionaryResult.Elements.Keys.Select(k => k.Value.ToString())
                .Should()
                .BeEquivalentTo(value.Keys);

            dictionaryResult.Elements.Values.Should()
                .BeEquivalentTo(value.Values.Select(v => new ScalarValue(v)));
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureDictionary_ValueIsMasked_MaskedValueIsDestructured(Dictionary<string, string> value)
        {
            // Arrange
            var maskedKey = value.Keys.First();
            var maskedValue = value[maskedKey];
            var expectedMaskedValue = maskedValue.Mask();

            var policy = new NamedValueHandlersBuilder()
                .Mask(maskedKey)
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (DictionaryValue)result;
            dictionaryResult.Elements.Keys.Should().Contain(e => e.Value.ToString() == maskedKey);

            dictionaryResult.Elements[new ScalarValue(maskedKey)]
                .ToString()
                .Should()
                .Contain(expectedMaskedValue);
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureDictionary_ValueIsOmitted_ByName_ValueIsRemoved(Dictionary<string, string> value)
        {
            // Arrange
            var omittedKey = value.Keys.First();

            var policy = new NamedValueHandlersBuilder()
                .Omit(omittedKey)
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (DictionaryValue)result;
            dictionaryResult.Elements.Keys.Should().NotContain(e => e.Value.ToString() == omittedKey);
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureDictionary_ValueIsOmitted_ByType_ValueIsRemoved(Dictionary<string, string> value)
        {
            // Arrange
            var omittedKey = value.Keys.First();
            var omittedValue = value[omittedKey];

            var policy = new NamedValueHandlersBuilder()
                .OmitType(omittedValue.GetType())
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (DictionaryValue)result;
            dictionaryResult.Elements.Keys.Should().NotContain(e => e.Value.ToString() == omittedKey);
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureDictionary_ValueIsOmitted_ByNamespace_ValueIsRemoved(Dictionary<string, string> value)
        {
            // Arrange
            var omittedKey = value.Keys.First();
            var omittedValue = value[omittedKey];
            var omittedNamespace = omittedValue.GetType().Namespace?.Split(".").First();

            var policy = new NamedValueHandlersBuilder()
                .OmitNamespace(omittedNamespace)
                .BuildDestructuringPolicy();

            // Act
            var isHandled = policy.TryDestructure(value, ScalarOnlyFactory, out var result);

            // Assert
            isHandled.Should().BeTrue();
            result.Should().NotBeNull();

            var dictionaryResult = (DictionaryValue)result;
            dictionaryResult.Elements.Keys.Should().NotContain(e => e.Value.ToString() == omittedKey);
        }

        [Theory]
        [AutoMoqData]
        public void TryDestructureDictionary_ValueIsNotMaskedOrOmitted_ValueIsNotChanged(Dictionary<string, string> value)
        {
            // Arrange
            var notModifiedKey = value.Keys.First();
            var notModifiedValue = value[notModifiedKey];

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

            var dictionaryResult = (DictionaryValue)result;
            dictionaryResult.Elements.Keys.Should().Contain(e => e.Value.ToString() == notModifiedKey);

            dictionaryResult.Elements[new ScalarValue(notModifiedKey)]
                .ToString()
                .Should()
                .Contain(notModifiedValue);
        }
    }
}
