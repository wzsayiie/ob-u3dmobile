const InputPBFilesDir = './definitions'
const OutputJavasFile = '../tsproj/manuscript/data/pb.js'
const OutputTypesFile = '../tsproj/manuscript/data/pb.d.ts'

const fs    = require('fs')
const npath = require('path')
const pbjs  = require('protobufjs/cli/pbjs')
const pbts  = require('protobufjs/cli/pbts')
const proc  = require('process')

function main() {
    enterCurrentDir()

    let files = []
    collectFiles(InputPBFilesDir, files)
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

function enterCurrentDir() {
    proc.chdir(__dirname)
}

/**
 * @param {string}   dir
 * @param {string[]} outFiles
 */
function collectFiles(dir, outFiles) {
    let subitems = fs.readdirSync(dir)
    for (let item of subitems) {
        let path = npath.join(dir, item)
        let stat = fs.statSync(path)

        if (stat.isDirectory()) {
            collectFiles(path, outFiles)
        } else if (path.endsWith('.proto')) {
            outFiles.push(path)
        }
    }
}

/**
 * @param {string[]}               files
 * @param {(error:Object) => void} callback
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
        '--path'  , InputPBFilesDir,
        '--out'   , OutputJavasFile,

        ...files
    ]

    pbjs.main(arguments, (error) => {
        callback(error)
    });
}

/**
 * @param {(error:Object) => void} callback
 */
function transferTypes(callback) {
    let arguments = [
        '--out', OutputTypesFile,
        OutputJavasFile
    ]

    pbts.main(arguments, (error) => {
        callback(error)
    })
}

main()
