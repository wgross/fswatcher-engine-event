namespace System.Management.Automation;

public static class PSObjectExtensions
{
    /// <summary>
    /// Reads the <see cref="PSObject.BaseObject"/> and casts it to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pso"></param>
    /// <returns></returns>
    public static T Unwrap<T>(this PSObject pso) => (T)pso.BaseObject;

    /// <summary>
    /// Reads the value <paramref name="name"/> from the given <see cref="PSObject"/> and casts it to <typeparamref name="V"/>.
    /// </summary>
    /// <typeparam name="V"></typeparam>
    /// <param name="obj"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static V Property<V>(this PSObject obj, string name) => (V)obj.Properties[name].Value;
}