﻿using FluentAssertions;
using Serilog.Sinks.Graylog.Core.Extensions;
using Xunit;

namespace Serilog.Sinks.Graylog.Core.Tests.Extensions
{
    public class StringExtensionsFixture
    {
        [Fact]
        public void WhenCompressMessage_ThenResultShoouldBeExpected()
        {
            var giwen = "Some string";
            var expected = new byte[]
            {
                31,139,8,0,0,0,0,0,0,11,11,206,207,77,85,40,46,41,202,204,75,7,0,142,183,209,127,11,0,0,0
            };

            byte[] actual = giwen.Compress();
            actual.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData("SomeTestString", "Some", 4)]
        [InlineData("SomeTestString", "SomeTest", 8)]
        [InlineData("SomeTestString", "SomeTestString", 200)]
        [InlineData("SomeTestString", "SomeT...", 8, "...")]
        public void WhenShortMessage_ThenResultShouldBeExpected(string given, string expected, int length, string postfix = "")
        {
            var actual = given.Truncate(length, postfix);

            actual.Should().BeEquivalentTo(expected);
        }
    }
}