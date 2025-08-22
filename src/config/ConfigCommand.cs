﻿using LethalSeedCracker3.src.common;
using System;

namespace LethalSeedCracker3.src.config
{
    internal abstract class BaseConfigCommand(string cmd, int numArgs)
    {
        public string cmd = cmd;
        internal virtual void Parse(Config config, string[] args)
        {
            Util.Assert(args.Length == numArgs, $"{cmd} expected {numArgs} arg, got {args.Length}");
        }
    }
    internal abstract class ConfigCommand(string cmd
        ) : BaseConfigCommand(cmd, 0)
    {
        internal sealed override void Parse(Config config, string[] args)
        {
            base.Parse(config, args);
            Process(config);
        }
        internal abstract void Process(Config config);
        public override string ToString()
        {
            return $"{cmd}";
        }
    }
    internal abstract class ConfigCommand<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0
        ) : BaseConfigCommand(cmd, 1)
    {
        internal sealed override void Parse(Config config, string[] args)
        {
            base.Parse(config, args);
            Process(config, parser0(config, args[0]));
        }
        internal abstract void Process(Config config, T0 arg0);
        public override string ToString()
        {
            return $"{cmd} <{name0}>";
        }
    }
    internal abstract class ConfigCommand<T0, T1>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1
        ) : BaseConfigCommand(cmd, 2)
    {
        internal sealed override void Parse(Config config, string[] args)
        {
            base.Parse(config, args);
            Process(config, parser0(config, args[0]), parser1(config, args[1]));
        }
        internal abstract void Process(Config config, T0 arg0, T1 arg1);
        public override string ToString()
        {
            return $"{cmd} <{name0}> <{name1}>";
        }
    }
    internal abstract class ConfigCommand<T0, T1, T2>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Func<Config, string, T2> parser2, string name2
        ) : BaseConfigCommand(cmd, 3)
    {
        internal sealed override void Parse(Config config, string[] args)
        {
            base.Parse(config, args);
            Process(config, parser0(config, args[0]), parser1(config, args[1]), parser2(config, args[2]));
        }
        internal abstract void Process(Config config, T0 arg0, T1 arg1, T2 arg2);
        public override string ToString()
        {
            return $"{cmd} <{name0}> <{name1}> <{name2}>";
        }
    }
}
