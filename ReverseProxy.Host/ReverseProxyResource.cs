namespace Aspire.Hosting.ApplicationModel;

public class ReverseProxyResource(string name) : ProjectResource(name)
{
    public readonly Dictionary<string, List<string>> routeMap = [];
}