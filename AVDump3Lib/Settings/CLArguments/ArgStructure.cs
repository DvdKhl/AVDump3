using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace AVDump3Lib.Settings.CLArguments {
    public class ArgStructure {
        public string[] Keys { get; private set; }
        public string Description { get; private set; }
        public string Example { get; private set; }
        public Action<string> ApplyCommand { get; private set; }

        public ArgStructure(IEnumerable<string> keys, Action<string> applyCommand, string example, string description) {
            Keys = keys.Where(ldKey => ldKey != null).OrderByDescending(ldKey => ldKey.Length).ToArray();
            ApplyCommand = applyCommand;
            Example = example;
            Description = description;
        }
        public static ArgStructure Create(Action<string> applyCommand, string example, string description, params string[] keys) {
            return new ArgStructure(keys, applyCommand, example, description);
        }

        public static ArgStructure Create<T>(Func<string, T> parseArgs, Action<T> applyCommand, string example, string description, params string[] keys) {
            return new ArgStructure(
                keys.Where(ldKey => ldKey != null).OrderByDescending(ldKey => ldKey.Length).ToArray(),
                arg => {
                    T transformedArg;
                    try {
                        transformedArg = parseArgs(arg);
                    } catch(Exception ex) {
                        throw new FormatException("Couldn't parse argument(" + arg + ") for ArgStructure(" + keys.First() + ")", ex);
                    }

                    try {
                        applyCommand(transformedArg);
                    } catch(Exception ex) {
                        throw new FormatException("Couldn't apply command for ArgStructure(" + keys.First() + "=" + arg + ")", ex);
                    }
                },
                example,
                description
            );
        }

        public static string[] SplitParams(string param) { return param.Split(new char[] { ':' }, 2); }

        public static IPEndPoint ParseHost(string param) {
            for(int i = 0; i < 5; i++) try { return new IPEndPoint(Dns.GetHostAddresses(param.Split(':')[0]).First(), int.Parse(param.Split(':')[1])); } catch(Exception) { }
            throw new Exception("Couldn't resolve host");
        }

        public static int ToInt(string param) { return int.Parse(param); }

        public static string PathParam(string param) {
            var path = Path.IsPathRooted(param) ? param : Path.Combine("", param);
            if(!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }

        public override string ToString() { return Keys.First(); }
    }
}
