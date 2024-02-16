
# Unifind
 
## Generic Fuzzy Finder for Unity Editor

Unifind is just a simple way to fuzzy-select from a list of items from inside a Unity editor script.

Usage
---

After adding to your project, by default you can open the fuzzy window either with `alt+d` or by selecting `Window` -> `General` -> `Fuzzy Finder`, after which you should see this popup:

<img src="screenshot.png?raw=true" alt="Unifind Screenshot"/>

From there, you can enter text to match against the list of actions that have been added.  It will use subsequence matching, so you only need enter the characters in the correct order and don't need to exactly match.

Custom Actions
---

There is a set of default actions that you can see in the screenshot above, but the main purpose of Unifind is to make it easy to define custom user actions.  You can do this by just defining a static method with the attribute `[FuzzyFinderMethod]` anywhere in your project.  For example:

```csharp
  using UnityEngine;
  using Unifind;

  public class Foo
  {
      [FuzzyFinderMethod]
      public static void Bar()
      {
          Debug.Log("hello world!");
      }
  }
```

You can also invoke the fuzzy finder manually from an existing editor script, or from a `FuzzyFinderMethod` (to create a nested menu) like this:

```csharp
  using UnityEngine;
  using Unifind;

  public class Foo
  {
      [FuzzyFinderMethod]
      public static async void Bar()
      {
          var widgets = new string[] { "Widget 1", "Widget 2", "Widget 3" };
          var choice = await FuzzyFinderWindow.Select("Choose Widget", widgets);
          Debug.LogFormat("You chose {0}", choice);
      }
  }
```

For more advanced usage see the default finders in the file `MiscFinders.cs`
