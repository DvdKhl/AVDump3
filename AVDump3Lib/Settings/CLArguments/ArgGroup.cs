using System;

namespace AVDump3Lib.Settings.CLArguments {
    public class ArgGroup {
        public string NameSpace { get; private set; }
        public string Description { get; private set; }
        public ArgStructure[] Args { get; private set; }
        public Action Parsed { get; private set; }

        public ArgGroup(string nameSpace, string description, params ArgStructure[] args) {
            NameSpace = nameSpace;
            Description = description;
            Args = (ArgStructure[])args.Clone();
        }
        public ArgGroup(string nameSpace, string description, Action parsed, params ArgStructure[] args)
            : this(nameSpace, description, args) {
            Parsed = parsed;
        }

        public override bool Equals(object obj) {
            if(obj == null || GetType() != obj.GetType()) return false;
            var argGroup = (ArgGroup)obj;
            return argGroup.NameSpace.Equals(NameSpace);
        }
        public override int GetHashCode() { return NameSpace.GetHashCode(); }
    }
}
