using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using hlcup2018.Models;

public class StorageLoader
{
  private readonly Storage storage;

  public StorageLoader(Storage storage) => this.storage = storage;
  
  private void DeserializeZip(IEnumerable<ZipArchiveEntry> entries)
  {
    var serializer = new JsonSerializer();

    foreach (var entry in entries)
    {
      using (var entryStream = entry.Open())
      using (var reader = new StreamReader(entryStream))
      using (var jsonTextReader = new JsonTextReader(reader))
      {
        var wrapper = serializer.Deserialize<Accounts>(jsonTextReader);
        foreach (var a in wrapper.accounts)
        {
          this.storage.AddAccount(a);
        }
      }
    }
  }

  public void Load(string path)
  {
    var dataPath = Path.Combine(path, "data.zip");
    var optionsPath = Path.Combine(path, "options.txt");
    if (!File.Exists(dataPath)) 
      Environment.Exit(1);

    if (!File.Exists(optionsPath)) 
      Environment.Exit(1);

    var ts = int.Parse(File.ReadLines(optionsPath).First());
    //this.storage.Timestamp = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
    this.storage.timestamp = ts;

    using (var archive = ZipFile.OpenRead(dataPath))
    {
      DeserializeZip(archive.Entries);
    }

    Console.WriteLine("Statistics:");
    Console.WriteLine("Accounts:{0}", this.storage.GetAllAccounts().Count());
    Console.WriteLine("Cities:{0}", this.storage.citiesMap.Count);
    Console.WriteLine("Countries:{0}", this.storage.countriesMap.Count);
    Console.WriteLine("First names:{0}", this.storage.firstNamesMap.Count);
    Console.WriteLine("Surnames:{0}", this.storage.surnamesMap.Count);
    Console.WriteLine("Interests:{0}", this.storage.interestsMap.Count);
    Console.WriteLine("Phone codes:{0}", this.storage.GetAllAccounts().Select(x => x.GetPhoneCode()).Distinct().Count());
    Console.WriteLine("Domains:{0}", this.storage.emailMap.GetAllDomains().Count());
    Console.WriteLine("Premium users:{0}", this.storage.GetAllAccounts().Where(a => a.MatchIsPremium(ts)).Count());
    Console.WriteLine("Has null premium:{0}", this.storage.GetAllAccounts().Where(a => a.MatchHasPremium(false)).Count());
    Console.WriteLine("Timestamp:{0}", this.storage.timestamp);

    System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
    GC.Collect(2, GCCollectionMode.Forced, true, true);
    GC.WaitForPendingFinalizers();
    GC.WaitForFullGCComplete();

    this.storage.BuildIndex();
  }

  public class Accounts { public Account[] accounts; }

}
