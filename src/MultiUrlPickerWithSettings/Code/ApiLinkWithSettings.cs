// Copyright (c) Umbraco.
// See LICENSE for more details.

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Cms.Core.PropertyEditors.ValueConverters;

public sealed class ApiLinkWithSettings
{
    public static ApiLinkWithSettings Content(string title, string? target, Guid destinationId, string destinationType, IApiContentRoute route, ApiBlockItem? setttings)
        => new(LinkType.Content, null, title, target, destinationId, destinationType, route, setttings);

    public static ApiLinkWithSettings Media(string title, string url, string? target, Guid destinationId, string destinationType, ApiBlockItem? setttings)
        => new(LinkType.Media, url, title, target, destinationId, destinationType, null, setttings);

    public static ApiLinkWithSettings External(string? title, string url, string? target, ApiBlockItem? setttings)
        => new(LinkType.External, url, title, target, null, null, null, setttings);

    private ApiLinkWithSettings(LinkType linkType, string? url, string? title, string? target, Guid? destinationId, string? destinationType, IApiContentRoute? route, ApiBlockItem? setttings)
    {
        LinkType = linkType;
        Url = url;
        Title = title;
        Target = target;
        DestinationId = destinationId;
        DestinationType = destinationType;
        Route = route;
        Setttings = setttings;
    }

    public string? Url { get; }

    public string? Title { get; }

    public string? Target { get; }

    public Guid? DestinationId { get; }

    public string? DestinationType { get; }

    public IApiContentRoute? Route { get; }

    public LinkType LinkType { get; }

    public ApiBlockItem? Setttings { get; }
}
