using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using GetDnsSrvRecord;

namespace TestUnit
{
    [TestFixture]
    public class Testclass
    {
        [Test]
        public void TestDnsSrvRecord()
        {
            var initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.Commands.Add(
                new SessionStateCmdletEntry("Get-DNSSrvRecord", typeof(GetDnsSrvRecord.GetDnsSrvRecord), null)
                );

            using (var runspace = RunspaceFactory.CreateRunspace(initialSessionState))
            {
                runspace.Open();
                using (var powershell = PowerShell.Create())
                {
                    powershell.Runspace = runspace;
                    var testcommand = new Command("Get-DNSSrvRecord");
                    testcommand.Parameters.Add("Domain", "j0rt3g4.com");

                    powershell.Commands.AddCommand(testcommand);

                    var results = powershell.Invoke();


                    Assert.AreEqual(results.Count(), 4);
                    Assert.IsNotNull(results[0].Properties["Name"]);
                    Assert.IsNotNull(results[2].Properties["Name"]);
                    Assert.IsNotNull(results[1].Properties["Name"]);
                   
                }
            }

        }
    }
}
