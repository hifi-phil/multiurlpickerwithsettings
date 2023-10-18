// Copyright (c) Umbraco.
// See LICENSE for more details.

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Cms.Core.PropertyEditors.ValueConverters;

public sealed class ApiLinkWithSettings
{
    public static ApiLinkWithSettings Content(string title, string? target, Guid destinationId, string destinationType, IApiContentRoute route, IApiElement? settings)
        => new(LinkType.Content, null, title, target, destinationId, destinationType, route, settings);

    public static ApiLinkWithSettings Media(string title, string url, string? target, Guid destinationId, string destinationType, IApiElement? settings)
        => new(LinkType.Media, url, title, target, destinationId, destinationType, null, settings);

    public static ApiLinkWithSettings External(string? title, string url, string? target, IApiElement? settings)
        => new(LinkType.External, url, title, target, null, null, null, settings);

    private ApiLinkWithSettings(LinkType linkType, string? url, string? title, string? target, Guid? destinationId, string? destinationType, IApiContentRoute? route, IApiElement? settings)
    {
        LinkType = linkType;
        Url = url;
        Title = title;
        Target = target;
        DestinationId = destinationId;
        DestinationType = destinationType;
        Route = route;
        Settings = settings;
    }

    public string? Url { get; }

    public string? Title { get; }

    public string? Target { get; }

    public Guid? DestinationId { get; }

    public string? DestinationType { get; }

    public IApiContentRoute? Route { get; }

    public LinkType LinkType { get; }

    public IApiElement? Settings { get; }
}
