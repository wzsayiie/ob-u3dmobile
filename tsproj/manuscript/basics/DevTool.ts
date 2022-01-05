import { Singleton } from './Singleton'
import { U3DMobile } from 'csharp'

export class DevTool extends Singleton<DevTool>() {

    public Install(): void {
        let puerts = require('puerts')

        //"source-map-support" need these functions to read sources.
        puerts.registerBuildinModule('path', {

            dirname: function (path: string): string {
                let end = path.lastIndexOf('\\')
                if (end == -1) {
                    end = path.lastIndexOf('/')
                }

                return path.substring(0, end)
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
                //sources always exist.
                return true
            },

            readFileSync: function (path: string): string {
                return U3DMobile.AssetManager.instance.LoadString(path)
            },
        })

        //on "inline-source-map" option, need "buffer" module.
        let global = <any> globalThis
        global['Buffer'] = require('buffer').Buffer

        require('source-map-support').install()
    }
}
