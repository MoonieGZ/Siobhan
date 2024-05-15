using Discord;
using Firefly.Helpers;

namespace Firefly.Extensions;

public static class EmbedExtensions
{
    public static EmbedBuilder MakeEmbed(Color color)
    {
        var embedBuilder = new EmbedBuilder
        {
            Color = color
            // Footer = MakeFooter()
        };

        return embedBuilder;
    }

    public static EmbedBuilder MakeErrorEmbed()
    {
        var embedBuilder = new EmbedBuilder
        {
            Color = Color.Red,
            Footer = MakeFooter(),
            ThumbnailUrl = Keys.Images.SadFace
        };

        return embedBuilder;
    }

    private static EmbedFooterBuilder MakeFooter()
    {
        return new EmbedFooterBuilder
        {
            Text = "Firefly",
            IconUrl = Keys.Images.Avatar
        };
    }
}
