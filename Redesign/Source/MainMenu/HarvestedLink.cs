using System;
using UnityEngine;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public sealed record HarvestedLink(string Label, Texture2D? Icon, Action OnClick);
