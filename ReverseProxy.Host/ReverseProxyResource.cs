namespace Aspire.Hosting.ApplicationModel;

public class ReverseProxyResource(string name) : ProjectResource(name)
{
    internal readonly Dictionary<string, List<string>> routeMap = [];
}