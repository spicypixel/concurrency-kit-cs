let gulp = require("gulp");
let gutil = require("gulp-util");
let msbuild = require("gulp-msbuild");
let nunit = require("gulp-nunit-runner");
let del = require("del");
let shell = require("gulp-shell");
let spawn = require("child_process").spawn;
let exec = require('child_process').exec;
let path = require("path");
let pathExists = require("path-exists");
let mkdirp = require('mkdirp');
let flatten = require("gulp-flatten");

import * as Promise from "bluebird";
import { ChildProcess } from "@spicypixel-private/core-kit-js/lib/child-process"
import { UnityEditor } from "@spicypixel-private/unity-kit-js/lib/unity-editor"

// Default task to run continuous integration
gulp.task("default", ["build"]);

// Rebuild is a clean followed by build
gulp.task("rebuild", () => clean().then(() => build()));

// Clean removes build artifacts
gulp.task("clean", () => clean());

// Build code
gulp.task("build:code", () => buildCode());

// Build docs
gulp.task("build:docs", () => buildDocs());

// Build code and docs
gulp.task("build", () => buildCode().then(() => buildDocs()));

// Test with NUnit
gulp.task("test", () => buildCode().then(() => test()));

// Install
gulp.task("install:monodocs", () => installMonoDocs());

function clean() {
  gutil.log(gutil.colors.cyan("Cleaning ..."));
  return del(["Source/**/bin/*", "Source/**/obj/*",
    "Doxygen/html", "Doxygen/xml", "Doxygen/qt",
    "MonoDoc/xml", "MonoDoc/assemble", "MonoDoc/html"]);
}

function build() {
  return buildCode().then(() => buildDocs());
}

// Build code
function buildCode() {
  gutil.log(gutil.colors.cyan("Bulding code ..."));

  return new Promise((resolve, reject) => {
    gulp
      .src("Source/**/*.sln")
      .pipe(msbuild({
        properties: {
          UnityEnginePath: UnityEditor.enginePath
        },
        toolsVersion: 12.0,
        targets: ["Build"],
        errorOnFail: true,
        stdout: true
      })).once("end", resolve)
      .once("error", reject);
  });
}

// Install node dependencies
function installDoxygenTheme() {
  gutil.log(gutil.colors.cyan("Installing doxygen theme ..."));

  return ChildProcess.spawnAsync("npm", ["install"],
    { cwd: path.join(__dirname, "Doxygen", "theme", "bootstrap"), log: true });
}

// Build Doxygen
function buildDoxygen() {
  gutil.log(gutil.colors.cyan("Building Doxygen docs ..."));
  return ChildProcess.spawnAsync("doxygen", [], { log: true });
}

// Update MonoDoc external docs
function updateMonoDoc() {
  let assemblies = [
    "System.Threading",
    "SpicyPixel.Threading",
    "SpicyPixel.Threading.Unity"];

  let assemblyPaths: string[] = [];
  assemblies.forEach(a => {
    assemblyPaths = assemblyPaths.concat(
      path.join(__dirname, "Source", a,
        "bin", "Release", a + ".dll"));
  });

  let xmlParams: string[] = [];
  assemblies.forEach(a => {
    xmlParams = xmlParams.concat("-i");
    xmlParams = xmlParams.concat(
      path.join(__dirname, "Source", a,
        "bin", "Release", a + ".xml"));
  });

  return ChildProcess.spawnAsync("mdoc", [
    "update",
    "-out:MonoDoc/xml",
  ].concat(xmlParams).concat(assemblyPaths), { log: true });
}

// Merge Doxygen to MonoDoc
function mergeDoxygenToMonoDoc() {
  gutil.log(gutil.colors.cyan("Merging docs ..."));
  let appDir = require.resolve("doxygentoecma")
    .match(/.*\/node_modules\/[^/]+\//)[0];
  let doxygentoecmaPath = path.join(appDir, "bin", "Release", "doxygentoecma.exe");

  return ChildProcess.spawnAsync("mono", [doxygentoecmaPath,
    path.join(__dirname, "Doxygen/xml"),
    path.join(__dirname, "MonoDoc/xml"), { log: true }]);
}

function assembleMonoDoc() {
  gutil.log(gutil.colors.cyan("Assembling MonoDocs ..."));

  let assembleDir = path.join(__dirname, "MonoDoc/assemble");
  mkdirp.sync(assembleDir);
  let prefix = path.join(assembleDir, "SpicyPixel.ConcurrencyKit");
  return ChildProcess.spawnAsync("mdoc", [
    "assemble", "-o", prefix, "MonoDoc/xml"
  ], { log: true });
}

function installMonoDocs() {
  return new Promise((resolve, reject) => {
    getMonoDocInstallPath((path: string) => {
      gulp
        .src(["MonoDoc/assemble/*", "MonoDoc/*.source"])
        .pipe(flatten())
        .pipe(gulp.dest(path))
        .on("end", resolve)
        .on("error", reject);
    });
  });
}

// Build docs
function buildDocs() {
  return installDoxygenTheme()
    .then(() => buildDoxygen())
    .then(() => updateMonoDoc())
    // .then(() => mergeDoxygenToMonoDoc())
    .then(() => assembleMonoDoc());
}

// Test with NUnit
function test() {
  return gulp
    .src(["**/bin/**/*Test.dll"], { read: false })
    .pipe(nunit({
      executable: "/usr/local/bin",
      teamcity: false
    }));
}

function execute(command: string, callback: Function): void {
  exec(command, function (error: Error, stdout: any, stderr: any) {
    callback(stdout);
  });
};

function getMonoDocInstallPath(callback: Function) {
  callback("/Library/Frameworks/Mono.framework/External/monodoc/");
  // execute("pkg-config monodoc --variable=sourcesdir", callback);
}