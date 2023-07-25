using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FontLoader.Core;

internal class RenderTargetHolder
{
    private static Queue<(string name, RenderTarget2D target, Action<RenderTarget2D> renderCallback)> _requestQueue;
    internal static Dictionary<string, RenderTarget2D> TargetLookup;

    internal static void Load() {
        _requestQueue = new Queue<(string name, RenderTarget2D target, Action<RenderTarget2D> renderCallback)>();
        TargetLookup = new Dictionary<string, RenderTarget2D>();

        On_Main.CheckMonoliths += orig => {
            while (_requestQueue.TryDequeue(out var request) && !TargetLookup.ContainsKey(request.name)) {
                request.renderCallback(request.target);
                TargetLookup.Add(request.name, request.target);
            }

            orig.Invoke();
        };
    }

    internal static void Unload() {
        TargetLookup = null;
        _requestQueue = null;
    }

    internal static void Clear() {
        foreach (var target in TargetLookup.Values.Where(target => target is {IsDisposed: false})) {
            target.Dispose();
        }

        TargetLookup.Clear();
    }

    
    internal static void AddRequest(string key, RenderTarget2D target, Action<RenderTarget2D> renderCallback) {
        _requestQueue.Enqueue((key, target, renderCallback));
    }
}