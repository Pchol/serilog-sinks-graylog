﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using Serilog.Sinks.Graylog.Core.Extensions;
using Serilog.Sinks.Graylog.Core.Helpers;

namespace Serilog.Sinks.Graylog.Core.MessageBuilders
{
    /// <summary>
    /// Message builder
    /// </summary>
    /// <seealso cref="IMessageBuilder" />
    public class GelfMessageBuilder : IMessageBuilder
    {
        
        private readonly string _hostName;
        private JsonSerializer _serializer;
        private const string DefaultGelfVersion = "1.1";
        protected GraylogSinkOptionsBase Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GelfMessageBuilder"/> class.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="options">The options.</param>
        public GelfMessageBuilder(string hostName, GraylogSinkOptionsBase options)
        {
            _hostName = hostName;
            _serializer = JsonSerializer.Create(options.SerializerSettings);
            Options = options;
        }

        /// <summary>
        /// Builds the specified log event.
        /// </summary>
        /// <param name="logEvent">The log event.</param>
        /// <returns></returns>
        public virtual JObject Build(LogEvent logEvent)
        {
            var messageTemplateTextFormatter = new MessageTemplateTextFormatter("{Message:l}");
            var writer = new StringWriter();
            messageTemplateTextFormatter.Format(logEvent, writer);
            string message = writer.ToString();

            if (Options.TruncateLongMessageSettings.Available)
            {
                message = message.Truncate(Options.TruncateLongMessageSettings.MaxLengthMessage, Options.TruncateLongMessageSettings.PostfixTruncatedMessage);
            }

            string shortMessage = message.Truncate(Options.ShortMessageMaxLength);

            var gelfMessage = new GelfMessage
            {
                Version = DefaultGelfVersion,
                Host = Options.Host ?? _hostName,
                ShortMessage = shortMessage,
                Timestamp = logEvent.Timestamp.ConvertToNix(),
                Level = LogLevelMapper.GetMappedLevel(logEvent.Level),
                StringLevel = logEvent.Level.ToString(),
                Facility = Options.Facility
            };

            if (message.Length > Options.ShortMessageMaxLength)
            {
                gelfMessage.FullMessage = message;
            }

            JObject jsonObject = JObject.FromObject(gelfMessage);
            foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties)
            {
                if (Options.ExcludeMessageTemplateProperties)
                {
                    var propertyTokens = Enumerable.OfType<PropertyToken>(logEvent.MessageTemplate.Tokens);
                    if (propertyTokens.Any(x => x.PropertyName == property.Key))
                    {
                        continue;
                    }
                }

                AddAdditionalField(jsonObject, property);
            }

            if (Options.IncludeMessageTemplate)
            {
                string messageTemplate = logEvent.MessageTemplate.Text;
                jsonObject.Add($"_{Options.MessageTemplateFieldName}", messageTemplate);
            }

            return jsonObject;
        }

        private void AddAdditionalField(IDictionary<string, JToken> jObject,
                                        KeyValuePair<string, LogEventPropertyValue> property,
                                        string memberPath = "")
        {
            string key = string.IsNullOrEmpty(memberPath)
                ? property.Key
                : $"{memberPath}.{property.Key}";

            switch (property.Value)
            {
                case ScalarValue scalarValue:
                    if (key.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "id_";
                    }

                    if (!key.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "_" + key;
                    }

                    if (scalarValue.Value == null)
                    {
                        jObject.Add(key, null);
                        break;
                    }

                    var shouldCallToString = ShouldCallToString(scalarValue.Value.GetType());

                    JToken value;
                    if (shouldCallToString)
                    {
                        var preparedStringValue = scalarValue.Value is string
                            ? scalarValue.ToString("l", null)
                            : scalarValue.ToString();

                        if (Options.TruncateLongMessageSettings.Available && preparedStringValue.Length >
                            Options.TruncateLongMessageSettings.MaxLengthPropertyMessage)
                        {
                            preparedStringValue = preparedStringValue.Truncate(
                                Options.TruncateLongMessageSettings.MaxLengthPropertyMessage,
                                Options.TruncateLongMessageSettings.PostfixTruncatedMessage);
                        }

                        value = JToken.FromObject(preparedStringValue, _serializer);
                    }
                    else
                    {
                        value = JToken.FromObject(scalarValue.Value, _serializer);
                    }

                    jObject.Add(key, value);
                    break;
                case SequenceValue sequenceValue:
                    var sequenceValueString = sequenceValue.ToString();

                    jObject.Add(key, sequenceValueString);
                    break;
                case StructureValue structureValue:
                    foreach (LogEventProperty logEventProperty in structureValue.Properties)
                    {
                        AddAdditionalField(jObject,
                                           new KeyValuePair<string, LogEventPropertyValue>(logEventProperty.Name, logEventProperty.Value),
                                           key);
                    }
                    break;
                case DictionaryValue dictionaryValue:
                    foreach (KeyValuePair<ScalarValue, LogEventPropertyValue> dictionaryValueElement in dictionaryValue.Elements)
                    {
                        var renderedKey = dictionaryValueElement.Key.ToString();
                        AddAdditionalField(jObject, new KeyValuePair<string, LogEventPropertyValue>(renderedKey, dictionaryValueElement.Value), key);
                    }
                    break;
            }
        }

        private bool ShouldCallToString(Type type)
        {
            bool isNumeric = type.IsNumericType();

            if (type == typeof(DateTime))
            {
                return false;
            }

            if (type.IsEnum)
            {
                return false;
            }

            if (isNumeric)
            {
                return false;
            }

            return true;
        }
    }
}