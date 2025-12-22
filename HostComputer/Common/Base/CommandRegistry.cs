using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Base
{
    public static class CommandRegistry
    {
        private static readonly List<CommandBase> _commands = new();

        public static void Register(CommandBase cmd)
        {
            _commands.Add(cmd);
        }

        public static void RefreshAll()
        {
            foreach (var cmd in _commands)
                cmd.RaiseCanExecuteChanged();
        }
    }
}
