using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

namespace GetDnsSrvRecord
{
    [Cmdlet(VerbsCommon.Get, "DNSSrvRecord")]
    public class GetDnsSrvRecord : Cmdlet
    {

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1, HelpMessage = "A well formed domain name should be provided")]
        [ValidatePattern(@"(?=^.{1,253}$)(^(((?!-)[a-zA-Z0-9-]{1,63}(?<!-))|((?!-)[a-zA-Z0-9-]{1,63}(?<!-)\.)+[a-zA-Z]{2,63})$)")]
        public string Domain { get; set; }

        [Parameter(Mandatory = false, ValueFromPipeline = true, HelpMessage = "SoloSRV is a bool value that tells the script just work on SRV records")]
        public SwitchParameter SoloSrv { get; set; }

        private StringBuilder OutputSrv { get; set; }
        private Collection<PSObject> OutputPs { get; set; }

        #region overridefunctions

        protected override void BeginProcessing()
        {
            WriteVerbose($"Domain to be queried: {Domain}");
        }
        protected override void ProcessRecord()
        {

            WriteVerbose("Start Processing");
            if (SoloSrv)
            {
                OutputSrv = GetSRV(Domain);
                PSObject returnon = TransformSBintoPso(OutputSrv);
                WriteObject(returnon);
            }
            else
            {
                OutputSrv = GetSRV(Domain);
                OutputPs = GetAllPS(Domain);
                OutputPs.Add(TransformSBintoPso(OutputSrv));
                foreach (PSObject psob in OutputPs)
                {
                    WriteObject(psob);
                }
            }
            WriteVerbose("End Processing");
        }
        protected override void EndProcessing()
        {
            WriteVerbose("Finished");
        }
        protected override void StopProcessing()
        {
            WriteObject("Interrupted, exiting now");
            base.StopProcessing();
        }
        #endregion

        #region Functions
        StringBuilder GetSRV(string domain)
        {
            string command = "/c nslookup -q=srv _autodiscover._tcp." + domain;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            StringBuilder temp = new StringBuilder();
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                temp.AppendLine(line);
            }
            Int16 lines = (Int16)Regex.Matches(temp.ToString(), Environment.NewLine).Count;
            if (lines <= 4)
            {
                temp.AppendLine("There's no DNS SRV record for " + domain);
            }
            return temp;
        }
        Collection<PSObject> GetAllPS(string domain)
        {
            PowerShell powerShellInstance = PowerShell.Create();
            string script = $"Resolve-DnsName {domain} -SERVER 8.8.8.8  -Type ALL | select *";
            powerShellInstance.AddScript(script);
            return powerShellInstance.Invoke();
        }

        private PSObject TransformSBintoPso(StringBuilder outputsrv)
        {
            string priority = "-";
            string weight = "-";
            string port = "-";
            bool found = false;
            String srvhostname = "-";

            Int16 lines = (Int16)Regex.Matches(outputsrv.ToString(), Environment.NewLine).Count;
            if (lines > 4)
            {
                found = true;
                Regex regexSrvHost = new Regex(@"[^ = ]+$");
                Match regexSrv = regexSrvHost.Match(outputsrv.ToString());

                if (regexSrv.Success)
                {
                    srvhostname = regexSrv.ToString();
                }


                MatchCollection mc = Regex.Matches(outputsrv.ToString(), @"[?:weight =][0-9]+");
                int i = 1;
                foreach (Match match in mc)
                {
                    foreach (Capture cp in match.Captures)
                    {
                        switch (i)
                        {
                            case 2:
                                priority = cp.Value;
                                break;
                            case 3:
                                weight = cp.Value;
                                break;
                            case 4:
                                port = cp.Value;
                                break;
                        }
                        i++;
                    }
                }
            }

            srvhostname = Regex.Replace(srvhostname, @"\t|\n|\r", "");
            port = Regex.Replace(port, @"\t| ", "");
            priority = Regex.Replace(priority, @"\t| ", "");
            port = Regex.Replace(port, @"\t| ", "");
            weight = Regex.Replace(weight, @"\t| ", "");

            PSObject transformed = new PSObject();
            transformed.Members.Add(new PSNoteProperty("QueryType", "SVR"));
            transformed.Members.Add(new PSNoteProperty("Found", found));
            transformed.Members.Add(new PSNoteProperty("Type", "SVR DNS"));
            transformed.Members.Add(new PSNoteProperty("Name", "SVR service Location for " + Domain));
            transformed.Members.Add(new PSNoteProperty("Port", port));
            transformed.Members.Add(new PSNoteProperty("Priority", priority));
            transformed.Members.Add(new PSNoteProperty("weight", weight));
            transformed.Members.Add(new PSNoteProperty("SRV hostname", srvhostname));
            return transformed;
        }
        #endregion
    }
}
