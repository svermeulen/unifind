
# Unifind

## Generic Fuzzy Finder for Unity Editor

Unifind is just a simple way to fuzzy-select from a list of items from inside a Unity editor script.

Installation
---

1. The package is available on the [openupm registry](https://openupm.com). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).
2. Execute the openum command.
    - ```
      openupm add com.svermeulen.unifind
      ```

Usage
---

After adding to your project, by default you can open the fuzzy window either with `alt+d` or by selecting `Window` -> `General` -> `Fuzzy Finder`.  After this appears, select "Unifind Examples" then you should see this:

<img src="screenshot.png?raw=true" alt="Unifind Screenshot"/>

From there, you can enter text to match against the list of actions.  It will use subsequence matching, so you only need enter the characters in the correct order and don't need to exactly match.

Custom Actions
---

There is a set of example actions that you can see in the screenshot above, but the main purpose of Unifind is to make it easy to define custom user actions.  The only default action is the "Unifind Examples" action.  This is because it's probably better for you to add your own actions that help with your specific workflow.

To add your own root action, you can define a static method anywhere in your project with the attribute `[FuzzyFinderAction]` anywhere in your project.  For example:

```csharp
using UnityEngine;
using Unifind;

public class Foo
{
    [FuzzyFinderAction]
    public static void Bar()
    {
        Debug.Log("hello world!");
    }
}
```

You can also invoke the fuzzy finder manually from an existing editor script, or from a `FuzzyFinderAction` (to create a nested menu) like this:

```csharp
using UnityEngine;
using Unifind;

public class Foo
{
    [FuzzyFinderAction]
    public static async void Bar()
    {
        var widgets = new string[] { "Widget 1", "Widget 2", "Widget 3" };
        var choice = await FuzzyFinder.UserSelect("Choose Widget", widgets);
        Debug.LogFormat("You chose {0}", choice);
    }
}
```

For more advanced usage see the default finders in the file `ExampleFinders.cs`

For common actions you might want to define a custom shortcut to execute it directly instead of via the root unifind menu, which you can do like this:

```csharp
using UnityEngine;
using Unifind;

public class Foo
{
    [FuzzyFinderAction]
    [MenuItem("MyMenu/Fuzzy Finders/Bar &s", false, -1000)]
    public static async void Bar()
    {
        var widgets = new string[] { "Widget 1", "Widget 2", "Widget 3" };
        var choice = await FuzzyFinder.UserSelect("Choose Widget", widgets);
        Debug.LogFormat("You chose {0}", choice);
    }
}
```

Here, we use both [FuzzyFinderAction] and [MenuItem] so it can be executed either via the root unifind menu or directly with ALT+S

Action Groups
---

Sometimes you might want to group a set of [FuzzyFinderAction] methods together.  You can do this by using a shared GroupId for these methods.  For example:

```csharp
[FuzzyFinderAction(Name = "Choose Foo")]
public static async void ChooseFoos()
{
    var choice = await FuzzyFinder.UserSelect("Fuzzy Finder", FuzzyFinder.GenerateEntriesForGroup("foo"));
    choice?.Value();
}

[FuzzyFinderAction(GroupId = "foo")]
public static void Foo1()
{
    Debug.Log("Selected Foo1");
}

[FuzzyFinderAction(GroupId = "foo")]
public static void Foo2()
{
    Debug.Log("Selected Foo2");
}
```

Now, if we open the root menu with ALT+D, we can select "Choose Foo" then select from all the methods declared with a GroupId of "foo"

Acknowledgements
---

* [Unity.Mx](https://github.com/jcs090218/Unity.Mx) for inspiration and some rendering code

