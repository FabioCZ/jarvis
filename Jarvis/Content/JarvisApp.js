var app = angular.module('JarvisApp', ['ngMaterial', 'ngMessages', 'ngRoute', 'lfNgMdFileInput']);

app.config(function ($routeProvider) {
    $routeProvider
        .when('/', { templateUrl: 'Content/main.html' })
        .when('/help', { templateUrl: 'Content/help.html' })
        .when('/grade', { templateUrl: 'Content/grade.html' })
        .when('/stats', { templateUrl: 'Content/stats.html' });

});

app.controller('AppCtrl', function ($scope, $mdDialog, $http) {

    $scope.showHelp = function (ev) {
        $mdDialog.show({
            clickOutsideToClose: true,

            scope: $scope, // use parent scope in template
            preserveScope: true, // do not forget this if use parent scope
            templateUrl: 'Content/help.html',

            controller: function DialogController($scope, $mdDialog) {
                $scope.cancel = function () {
                    $mdDialog.hide();
                }
            }
        });
    };

$http({
        method: 'GET',
        url: '/version'
    }).then(function success(res) {
        $scope.jarvisVersion = res.data.version;

    }, function err(res) {
        console.log(res);
        $scope.jarvisVersion = "Unknown";
    });
});

app.controller('UploadCtrl', function($scope, $http) {
    $scope.submit = function () {
        var formData = new FormData();
        var file = $scope.fileToUpload[0];
        console.log(file);
        formData.append('file', file.lfFile, 'thefile');
        $http.post('/run', formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        }).then(function (result) {
            console.log(result);
        }, function (err) {
            console.log(error);
        });
    }
});

