using System;

using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Bot.Misc;

/// <inheritdoc cref="System.Attribute" />
public class HttpHeaderAttribute : Attribute, IActionConstraint
{
    /// <summary>
    ///
    /// </summary>
    public string Header { get; set; }
    /// <summary>
    ///
    /// </summary>
    public string Value { get; set; }

    /// <inheritdoc />
    public HttpHeaderAttribute (string header, string value)
    {
        Header = header;
        Value  = value;
    }

    /// <inheritdoc />
    public bool Accept(ActionConstraintContext context)
    {
        if (context.RouteContext.HttpContext.Request.Headers.TryGetValue(Header, out var value))
        {
            return value[0] == Value;
        }

        return false;
    }

    /// <inheritdoc />
    public int Order => 0;
}