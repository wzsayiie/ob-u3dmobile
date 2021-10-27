const InputDefinesDir = './defines'
const OutputJavasFile = '../tsproj/manuscript/data/pb.js'
const OutputTypesFile = '../tsproj/manuscript/data/pb.d.ts'

const $fs   = require('fs')
const $path = require('path')
const $pbjs = require('protobufjs/cli/pbjs')
const $pbts = require('protobufjs/cli/pbts')
const $proc = require('process')

/** @returns {void} */
function main() {
    gotoCurrentDir()

    let files = collectFiles(InputDefinesDir)
    if (files.length == 0) {
        console.log('not found any proto files')
        return
    }

    transferJavas(files, (error) => {
        if (error) {
            console.log(error)
            return
        }

        transferTypes((error) => {
            if (error) {
                console.log(error)
            }
        })
    })
}

/** @returns {void} */
function gotoCurrentDir() {
    $proc.chdir(__dirname)
}

/**
 * @param   {string  } dir
 * @returns {string[]}
 */
function collectFiles(dir) {
    let files = []

    let subitems = $fs.readdirSync(dir)
    for (let item of subitems) {
        let path = $path.join(dir, item)
        let stat = $fs.statSync(path)

        if (stat.isDirectory()) {
            let rest = collectFiles(path)
            files = [ ...files, ...rest ]

        } else if (path.endsWith('.proto')) {
            files.push(path)
        }
    }

    return files
}

/**
 * @param   {string[]}               files
 * @param   {(error:Object) => void} callback
 * @returns {void}
 */
function transferJavas(files, callback) {
    let arguments = [
        '--no-beautify' ,
        '--no-convert'  ,
        '--no-create'   ,
        '--no-delimited',
        '--no-service'  ,
        '--no-typeurl'  ,
        '--no-verify'   ,

        '--target', 'static-module',
        '--wrap'  , 'es6',
        '--path'  , InputDefinesDir,
        '--out'   , OutputJavasFile,

        ...files
    ]

    $pbjs.main(arguments, (error) => {
        callback(error)
    });
}

/**
 * @param   {(error:Object) => void} callback
 * @returns {void}
 */
function transferTypes(callback) {
    let arguments = [
        '--out', OutputTypesFile,
        OutputJavasFile
    ]

    $pbts.main(arguments, (error) => {
        callback(error)
    })
}

main()
