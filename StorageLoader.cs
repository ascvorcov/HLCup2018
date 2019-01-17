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
    if (!File.Exists(dataPath)) return;
    if (!File.Exists(optionsPath)) return;

    var ts = int.Parse(File.ReadLines(optionsPath).First());
    //this.storage.Timestamp = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
    this.storage.timestamp = ts;

    using (var archive = ZipFile.OpenRead(dataPath))
    {
      DeserializeZip(archive.Entries);
    }


    Console.WriteLine("Statistics:");
    Console.WriteLine("Accounts:{0}", Storage.Instance.GetAllAccounts().Count());
    Console.WriteLine("Cities:{0}", Storage.Instance.citiesMap.Count);
    Console.WriteLine("Countries:{0}", Storage.Instance.countriesMap.Count);
    Console.WriteLine("First names:{0}", Storage.Instance.firstNamesMap.Count);
    Console.WriteLine("Surnames:{0}", Storage.Instance.surnamesMap.Count);
    Console.WriteLine("Interests:{0}", Storage.Instance.interestsMap.Count);
    Console.WriteLine("Phone codes:{0}", Storage.Instance.GetAllAccounts().Select(x => x.GetPhoneCode()).Distinct().Count());
    Console.WriteLine("Domains:{0}", Storage.Instance.emailMap.GetAllDomains().Count());
    Console.WriteLine("Premium users:{0}", Storage.Instance.GetAllAccounts().Where(a => a.MatchIsPremium(ts)).Count());
    Console.WriteLine("Has null premium:{0}", Storage.Instance.GetAllAccounts().Where(a => a.MatchHasPremium(false)).Count());
    Console.WriteLine("Timestamp:{0}", Storage.Instance.timestamp);


    /*int[] interests = new int[Storage.Instance.interestsMap.Count - 1];
    foreach (var account in Storage.Instance.GetAllAccounts())
    {
      foreach (var id in account.GetInterestIds()) interests[id - 1]++;
    }
    Console.WriteLine("Average/Min/Max selectivity of interests: {0}/{1}/{2}", 
    interests.Average(), 
    interests.Min(), 
    interests.Max());*/


    System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
    GC.Collect(2, GCCollectionMode.Forced, true, true);
    GC.WaitForPendingFinalizers();
    GC.WaitForFullGCComplete();

    Console.WriteLine("Started building index");
    Storage.Instance.BuildIndex();
    Console.WriteLine("Index ready");
    Console.WriteLine(Storage.Instance.GetLikesStats());
  }

  public class Accounts { public Account[] accounts; }

}