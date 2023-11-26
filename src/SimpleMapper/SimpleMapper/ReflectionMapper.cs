using System.Reflection;

namespace SimpleMapper
{
    public class ReflectionMapper : IMapper
    {
        static object _lock = new object();

        private static Dictionary<(Type from, Type to), List<(MethodInfo Get, MethodInfo Set)>> _cache = new();

        private static List<(MethodInfo Get, MethodInfo Set)> GetOrAdd((Type from, Type to) key)
        {
            if (_cache.ContainsKey(key))
            {
                return _cache[key];
            }

            lock (_lock)
            {
                if (_cache.ContainsKey(key))
                {
                    return _cache[key];
                }

                var fromProps = key.from.GetProperties();
                var toProps = key.to.GetProperties();

                List<(MethodInfo, MethodInfo)> entry = new();
                foreach (var from in fromProps)
                {
                    var to = toProps.FirstOrDefault(x => x.Name == from.Name);
                    if (to == null)
                    {
                        continue;
                    }

                    entry.Add((from.GetMethod, to.SetMethod));
                }

                _cache[key] = entry;
            }

            return _cache[key];
        }

        public TTarget Map<TSource, TTarget>(TSource source) where TTarget : class, new()
        {
            var result = new TTarget();
            Map(source, result);
            return result;
        }

        public void Map<TSource, TTarget>(TSource source, TTarget target) where TTarget : class
        {
            var key = (from: source.GetType(), to: typeof(TTarget));

            var entry = GetOrAdd(key);
            foreach (var (Get, Set) in entry)
            {
                var val = Get.Invoke(source, null);
                Set.Invoke(target, [val]);
            }
        }
    }
}
