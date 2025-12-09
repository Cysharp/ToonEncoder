using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace ToonEncoder.Tests;

// https://toonformat.dev/guide/getting-started.html
public class GettingStarted
{
    static string JsonToToon([StringSyntax("json")] string json)
    {
        var jsonElement = JsonElement.Parse(json);
        return Cysharp.AI.ToonEncoder.Encode(jsonElement).Replace("\n", Environment.NewLine);
    }

    [Test]
    public async Task WhyToon()
    {
        var toon = JsonToToon("""
{
  "users": [
    { "id": 1, "name": "Alice", "role": "admin" },
    { "id": 2, "name": "Bob", "role": "user" }
  ]
}
""");

        await Assert.That(toon).IsEqualTo("""
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
""");
    }

    [Test]
    public async Task WhyToonNested()
    {
        var toon = JsonToToon("""
{
  "context": {
    "task": "Our favorite hikes together",
    "location": "Boulder",
    "season": "spring_2025"
  },
  "friends": ["ana", "luis", "sam"],
  "hikes": [
    {
      "id": 1,
      "name": "Blue Lake Trail",
      "distanceKm": 7.5,
      "elevationGain": 320,
      "companion": "ana",
      "wasSunny": true
    },
    {
      "id": 2,
      "name": "Ridge Overlook",
      "distanceKm": 9.2,
      "elevationGain": 540,
      "companion": "luis",
      "wasSunny": false
    },
    {
      "id": 3,
      "name": "Wildflower Loop",
      "distanceKm": 5.1,
      "elevationGain": 180,
      "companion": "sam",
      "wasSunny": true
    }
  ]
}
""");

        await Assert.That(toon).IsEqualTo("""
context:
  task: Our favorite hikes together
  location: Boulder
  season: spring_2025
friends[3]: ana,luis,sam
hikes[3]{id,name,distanceKm,elevationGain,companion,wasSunny}:
  1,Blue Lake Trail,7.5,320,ana,true
  2,Ridge Overlook,9.2,540,luis,false
  3,Wildflower Loop,5.1,180,sam,true
""");

    }
}
