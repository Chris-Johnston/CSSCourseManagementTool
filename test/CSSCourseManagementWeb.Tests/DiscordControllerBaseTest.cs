using CSSCourseManagementWeb.Controllers;
using System;
using Xunit;

namespace CSSCourseManagementWeb.Tests
{
    public class DiscordControllerBaseTest
    {
        /// <summary>
        ///     Tests <see cref="DiscordControllerBase.NormalizeCourseChannelName(string)"/> with expected input to ensure normal behavior.
        /// </summary>
        [Fact]
        public void TestNormalizeCourseChannelName_Expected()
        {
            Assert.Equal("css101", DiscordControllerBase.NormalizeCourseChannelName("css101"));
        }

        /// <summary>
        ///     Tests <see cref="DiscordControllerBase.NormalizeCourseChannelName(string)"/> with expected input to ensure the string
        ///     is trimmed if it's longer than 30 chars.
        /// </summary>
        [Fact]
        public void TestNormalizeCourseChannelName_TrimsCharacters()
        {
            // input is 40 chars, expect 30 chars
            Assert.Equal(
                "012345678901234567890123456789",
                DiscordControllerBase.NormalizeCourseChannelName("0123456789012345678901234567890123456789"));
        }

        /// <summary>
        ///     Tests that whitespace characters are converted to underscores.
        /// </summary>
        [Fact]
        public void TestNormalizeCourseChannelName_FiltersWhitespace()
        {
            Assert.Equal("bla_bla_bla", DiscordControllerBase.NormalizeCourseChannelName("bla bla bla"));
        }
    }
}
