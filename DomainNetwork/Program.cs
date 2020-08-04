using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainNetwork
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var k = System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain();
                Console.WriteLine("Domain Network found");
                Console.WriteLine($"Forest: {k.Forest}");
                Console.WriteLine($"Domain Mode: {k.DomainMode}");
                Console.WriteLine($"Domain Mode Level: {k.DomainModeLevel}");
                Console.WriteLine($"Name: {k.Name}");
                Console.WriteLine($"Pdc Role Owner: {k.PdcRoleOwner}");
                Console.WriteLine($"Rid Role Owner: {k.RidRoleOwner}");
            }
            catch
            {
                Console.WriteLine("Not connected to domain network");
            }
            Console.WriteLine("Press any button to exit");
            Console.ReadKey();
        }
    }
}
