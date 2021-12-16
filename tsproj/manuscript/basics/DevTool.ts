import { Singleton } from './Singleton'

export class DevTool extends Singleton<DevTool>() {

    public Install(): void {
        let csharp = require('csharp')
        let puerts = require('puerts')

        //"source-map-support" need these functions to read sources.
        puerts.registerBuildinModule('path', {

            dirname: function (path: string): string {
                return csharp.System.IO.Path.GetDirectoryName(path)
            },

            resolve: function (dir: string, url: string): string {
                let prefix = 'webpack://u3dmobile/manuscript/'
                if (url.startsWith(prefix)) {
                    return url.substring(prefix.length)
                } else {
                    return url
                }
            },
        })
        puerts.registerBuildinModule('fs', {

            existsSync: function (path: string): boolean {
                return csharp.System.IO.File.Exists(path)
            },

            readFileSync: function (path: string): string {
                return csharp.System.IO.File.ReadAllText(path)
            },
        })

        //on "inline-source-map" option, need "buffer" module.
        globalThis['Buffer'] = require('buffer').Buffer

        require('source-map-support').install()
    }
}
