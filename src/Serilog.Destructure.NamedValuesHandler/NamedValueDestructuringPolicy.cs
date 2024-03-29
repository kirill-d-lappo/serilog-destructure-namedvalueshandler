﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Destructure.NamedValuesHandler
{
    internal class NamedValueDestructuringPolicy : IDestructuringPolicy
    {
        private const string RootValueName = "root-value-object";

        private readonly OmitHandler        _omitHandler;
        private readonly NamedValuesHandler _namedValuesHandler;

        public NamedValueDestructuringPolicy(NamedValuesHandler namedValuesHandler, OmitHandler omitHandler)
        {
            _namedValuesHandler = namedValuesHandler;
            _omitHandler        = omitHandler;
        }

        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            if (value == null)
            {
                result = null;
                return false;
            }

            if (TryDestructureRootValue(value, propertyValueFactory, out result))
            {
                return true;
            }

            switch (value)
            {
                case DateTime: // Todo [2021/09/10 KL] Need to figure it our a better way to ignore non-key-value structures
                case Guid:
                case ValueType:
                    result = null;
                    return false;

                case IDictionary dictionary:
                    return TryDestructureDictionary(dictionary, propertyValueFactory, out result);

                case IEnumerable enumerable:
                    return TryDestructureEnumerable(enumerable, propertyValueFactory, out result);

                default:
                    return TryDestructureObject(value, propertyValueFactory, out result);
            }
        }

        private bool TryDestructureEnumerable(IEnumerable values, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            var handleResults = values.Cast<object>()
                .Select(v => new NamedValue(name: null, v, v.GetType()))
                .Where(v => !_omitHandler.IsOmitted(v))
                .Select(v => _namedValuesHandler.HandleNamedValue(v))
                .ToList();

            if (!handleResults.Any(r => r.IsHandled))
            {
                result = null;
                return false;
            }

            var propertyValues = handleResults
                // Bug [2021/09/24 KL] CreatePropertyValue returns nulls instead of destructuring values - we have a failing test
                .Select(r => propertyValueFactory.CreatePropertyValue(r.Value, destructureObjects: true))
                .ToList();

            result = new SequenceValue(propertyValues);
            return true;
        }

        private bool TryDestructureRootValue(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            var type = value?.GetType() ?? typeof(object);
            var namedValue = new NamedValue(RootValueName, value, type);
            if (_omitHandler.IsOmitted(namedValue))
            {
                result = null;
                return false;
            }

            var (isHandled, newValue) = _namedValuesHandler.HandleNamedValue(namedValue);
            result = isHandled
                ? propertyValueFactory.CreatePropertyValue(newValue, destructureObjects: true)
                : null;

            return isHandled;
        }

        private bool TryDestructureObject(
            object objectValue,
            ILogEventPropertyValueFactory propertyValueFactory,
            out LogEventPropertyValue result
        )
        {
            var type = objectValue.GetType();
            var propertyInfos = type.GetProperties();
            if (propertyInfos.Length <= 0)
            {
                result = null;
                return false;
            }

            var namedValues = propertyInfos
                .Select(
                    pi =>
                    {
                        var name = pi.Name;
                        var value = pi.GetValue(objectValue);
                        var valueType = pi.PropertyType;
                        return new NamedValue(name, value, valueType);
                    });

            var logEventProperties = DestructureNamedValues(namedValues, propertyValueFactory)
                .Select(_ => new LogEventProperty(_.name, _.logEventValue));

            result = new StructureValue(logEventProperties, type.FullName);
            return true;
        }

        private bool TryDestructureDictionary(
            IDictionary dictionary,
            ILogEventPropertyValueFactory propertyValueFactory,
            out LogEventPropertyValue result
        )
        {
            var namedValues = dictionary.Keys
                .Cast<object>()
                .Select(
                    k =>
                    {
                        var name = propertyValueFactory.CreatePropertyValue(k, destructureObjects: true)
                            .ToString()
                            .Trim(trimChar: '"');

                        var value = dictionary[k];
                        var valueType = value.GetType();
                        return new NamedValue(name, value, valueType);
                    });

            var logEventProperties = DestructureNamedValues(namedValues, propertyValueFactory)
                .Select(_ => new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(_.name), _.logEventValue));

            result = new DictionaryValue(logEventProperties);
            return true;
        }

        private IEnumerable<(string name, LogEventPropertyValue logEventValue)> DestructureNamedValues(
            IEnumerable<NamedValue> namedValues,
            ILogEventPropertyValueFactory propertyValueFactory
        )
        {
            return namedValues
                .Where(nv => !_omitHandler.IsOmitted(nv))
                .Select(
                    nv =>
                    {
                        var (isHandled, handledValue) = _namedValuesHandler.HandleNamedValue(nv);
                        var newValue = isHandled
                            ? handledValue
                            : nv.Value;

                        var logEventProperty = CreateEventPropertyValue(newValue, propertyValueFactory);
                        return (nv.Name, logEventProperty);
                    });
        }

        private static LogEventPropertyValue CreateEventPropertyValue(
            object value,
            ILogEventPropertyValueFactory propertyValueFactory
        )
        {
            return value == null
                ? new ScalarValue(value: null)
                : propertyValueFactory.CreatePropertyValue(value, destructureObjects: true);
        }
    }
}
