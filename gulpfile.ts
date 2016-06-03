import * as gulp from "gulp";
let gutil = require("gulp-util");
let msbuild = require("gulp-msbuild");
let nunit = require("gulp-nunit-runner");

import * as FileSystem from "@spicypixel-private/core-kit-js/lib/file-system";
import ChildProcess from "@spicypixel-private/core-kit-js/lib/child-process";
import UnityEditor from "@spicypixel-private/unity-kit-js/lib/unity-editor";
import * as BuildKit from "@spicypixel-private/build-kit-js";

let monoDocBuilder = new BuildKit.MonoDocBuilder();

gulp.task("default", () => build());
gulp.task("clean", () => clean());
gulp.task("rebuild", () => clean().then(() => build()));
gulp.task("build:code", () => buildCode());
gulp.task("build:docs", () => buildDocs());
gulp.task("build", () => build());
gulp.task("test", () => buildCode().then(() => test()));
gulp.task("install:monodocs", () => installMonoDocs());

async function clean() {
  gutil.log(gutil.colors.cyan("Cleaning ..."));

  await FileSystem.removePatternsAsync([
    "Source/**/bin/*", "Source/**/obj/*",
    "Doxygen/html", "Doxygen/xml", "Doxygen/qt",
    "MonoDoc/xml", "MonoDoc/assemble", "MonoDoc/html"]);
}

async function build() {
  await buildCode();
  await buildDocs();
}

async function buildCode() {
  gutil.log(gutil.colors.cyan("Bulding code ..."));

  await new Promise((resolve, reject) => {
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

async function installMonoDocs() {
  await monoDocBuilder.installAsync();
}

async function buildDocs() {
  await BuildKit.DoxygenBuilder.buildAsync();

  let assemblies = [
    "System.Threading",
    "SpicyPixel.Threading",
    "SpicyPixel.Threading.Unity"];
  await monoDocBuilder.buildAsync("SpicyPixel.ConcurrencyKit", assemblies);
}

function test() {
  return gulp
    .src(["**/bin/**/*Test.dll"], { read: false })
    .pipe(nunit({
      executable: "/usr/local/bin",
      teamcity: false
    }));
}