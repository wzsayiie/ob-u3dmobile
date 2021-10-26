const indir = './defines'
const outjs = '../tsproj/manuscript/data/pb.js'
const outts = '../tsproj/manuscript/data/pb.d.ts'

const $fs   = require('fs')
const $path = require('path')
const $pbjs = require('protobufjs/cli/pbjs')
const $pbts = require('protobufjs/cli/pbts')
const $proc = require('process')

function main() {
    $proc.chdir(__dirname)

    let files = allfiles(indir)
    if (files.length == 0) {
        console.log('not found any proto files')
        return
    }

    genjs(files, (err) => {
        if (err) {
            console.log(err)
            return
        }

        gents((err) => {
            if (err) {
                console.log(err)
            }
        })
    })
}

function allfiles(dir, out) {
    let files = out ? out : []

    let subitems = $fs.readdirSync(dir)
    for (let item of subitems) {
        let path = $path.join(dir, item)
        let stat = $fs.statSync(path)

        if (stat.isDirectory()) {
            allfiles(path, files)
        } else if (path.endsWith('.proto')) {
            files.push(path)
        }
    }

    return files
}

function genjs(files, callback) {
    let args = [
        '--no-beautify' ,
        '--no-convert'  ,
        '--no-create'   ,
        '--no-delimited',
        '--no-service'  ,
        '--no-typeurl'  ,
        '--no-verify'   ,

        '--target', 'static-module',
        '--wrap'  , 'es6',
        '--path'  , indir,
        '--out'   , outjs,

        ...files
    ]

    $pbjs.main(args, (err) => {
        callback(err)
    });
}

function gents(callback) {
    let args = [
        '--out', outts,
        outjs
    ]

    $pbts.main(args, (err) => {
        callback(err)
    })
}

main()
