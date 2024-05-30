﻿using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.YamlSerializer;

namespace Ship_Game;

public sealed class SaveBlueprintsScreen : GenericLoadSaveScreen
{
    readonly BlueprintsTemplate Blueprints;
    readonly BlueprintsScreen Screen;
    readonly string ModName;
    static SubTexture BlueprintsIcon = ResourceManager.Texture("NewUI/blueprints");

    public SaveBlueprintsScreen(BlueprintsScreen parent, BlueprintsTemplate blueprints)
        : base(parent, SLMode.Save, blueprints.Name, "Save Blueprints As...", "Colony Blueprints", "Saved Blueprints exists. " +
            "If you choose to overwrite, planets with these Blueprints will be reloaded with the new version. Overwrite?", 40)
    {
        ModName = BlueprintsTemplate.CurrentModName;
        Blueprints = blueprints;
        Path = Dir.StarDriveAppData + "/Colony Blueprints/" + ModName + "/";
        Screen = parent;
        if (!Directory.Exists(Path))
            Directory.CreateDirectory(Path);
    }

    public override void DoSave()
    {
        try
        {
            string name = EnterNameArea.Text;
            string path = Path + name + ".yaml";
            Blueprints.Name = name;
            if (Blueprints.LinkTo == name)
                Blueprints.LinkTo = ""; // avoid cyclic link for new blueprints

            YamlSerializer.SerializeOne(path, Blueprints);
            ResourceManager.AddBlueprintsTemplate(Blueprints);
            Screen.AfterBluprintsSave(Blueprints);
        }
        catch (Exception e)
        {
            Log.Error(e, "Save Blueprints Failed");
        }
        finally
        {
            ExitScreen();
        }
    }

    FileData CreateBlueprintsSaveItem(FileInfo info, BlueprintsTemplate blueprints)
    {
        return new(info, info, blueprints.Name, "", "", "", BlueprintsIcon, HelperFunctions.GetBlueprintsIconColor(blueprints)) 
        { Enabled = blueprints.Validated };
    }

    protected override void InitSaveList()
    {
        Array<FileData> items = new();
        string modName = BlueprintsTemplate.CurrentModName;
        foreach (FileInfo info in Dir.GetFiles(Path, "yaml"))
        {
            var blueprints = YamlParser.DeserializeOne<BlueprintsTemplate>(info);
            if (modName == blueprints.ModName)
                items.Add(CreateBlueprintsSaveItem(info, blueprints));
        }

        AddItemsToSaveSL(items);
    }
}
