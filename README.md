# SuperPrefabReplacement
An example of replacing Tiled objects with prefabs, automatically, during import into a Unity project. Fans of retro games may notice an inspiration from older Mega Man games. :)

Often a Tiled map will have objects in it that we want transformed into gameplay-specific objects when the map is imported into Unity.

In this example we have a series of blocks, represented as rectangle objects of type `AppearingBlock` in our map.

![Example Map](./docs/map.png)

![Appear Blocks Type](./docs/appear-block-type.png)

What we want is to take these placeholders in our Tiled map and replace them with prefab instances in Unity. This is where **Prefab Replacement** come into play.

Prefab Replacements
-------------------
Prefab Replacements are managed in the `SuperTiled2Unity Settings`, which are found in your `Project Settings` diaglog.

![SuperTiled2Unity Project Settings](./docs/st2u-project-settings.png)

What we want to do here is add an entry that replaces `AppearingBlock` objects in your Tiled map with some prefab. In the example provided, we use `AppearingBlockPrefab`.

![Replacing Appearing Blocks](./docs/aprearing-block-replacement.gif)

Now every time a Tiled map in your Unity project is imported it will replace `AppearingBlock` objects with `AppearingBlockPrefab` instances.

![Appearing Blocks Imported](./docs/appearing-blocks-replaced.png)

> **Tip**: Changes to your Prefab Replacements are not autoumatically applied to Tiled maps in your project. You will need to resave your Tiled map file (easiest and fastest) or use the `Reimport Tiled Assets` button in the SuperTiled2Unity settings window.

Custom Properties Supported
---------------------------

This example includes scripting that groups our appearing blocks and shows them in order. The grouping order is set in Tiled using [Custom Properties](https://doc.mapeditor.org/en/stable/manual/custom-properties/).

> **Tip**: See the SuperTiled2Unity documentation for advanced [Custom Properties support](https://supertiled2unity.readthedocs.io/en/latest/manual/custom-properties.html).

