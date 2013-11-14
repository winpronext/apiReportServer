/* main: startup script creates the 'app' module */

(function (window, angular, toastr) {
    'use strict';
    
    //#region configure toastr
    toastr.options.closeButton = true;
    toastr.options.newestOnTop = false;
    toastr.options.positionClass = 'toast-bottom-right';
    //#endregion

    // 'app' is the one Angular (Ng) module in this app
    // 'app' module is in global namespace
    window.app = angular.module('app', [
        //ng modules
        'ngRoute',
        'ngAnimate',
        'ngResource',
        //custom modules
        'services',
        'directives',
        'resources',
        'controllers',
        'http-auth-interceptor',
        'interceptors'
    ]);

    var exceptionHandler = function (e) {
        toastr.error(e.message || e);
    };

    // Learn about Angular dependency injection in this video
    // http://www.youtube.com/watch?feature=player_embedded&v=1CpiB3Wk25U#t=2253s
    app.value('$exceptionHandler', exceptionHandler);

    //#region Configure routes
    app.config(['$routeProvider', '$locationProvider', 
        function($routeProvider, $locationProvider) {
            $routeProvider.
                when('/home', { templateUrl: 'App/views/home.html', controller: 'HomeCtrl' }).
                when('/todos',
                    {
                        templateUrl: 'App/views/todos.html',
                        controller: 'TodosCtrl',
                        resolve: {
                            authentication: ['$http', function ($http) {
                                return $http.get('api/Account/Ping');
                            }]
                        }
                    }).
                when('/about', { templateUrl: 'App/views/about.html', controller: 'AboutCtrl' }).
                when('/settings',
                    {
                        templateUrl: 'App/views/settings.html',
                        controller: 'SettingsCtrl',
                        resolve: {
                            authentication: ['$http', function($http) {
                                return $http.get('api/Account/Ping');
                            }]
                        }
                    }).
                when('/logs', { templateUrl: 'App/views/logs.html', controller: 'LogsCtrl' }).
                otherwise({ redirectTo: '/home' });

            $locationProvider.html5Mode(false).hashPrefix('!');
        }]);
    //#endregion

    app.run(['$rootScope', '$location', '$window', '$auth',
        function($rootScope, $location, $window, $auth) {
            $rootScope.today = new Date();

            $auth.loadSaved().then(function (data) {
                $rootScope.userName = data.userName;
            });

            $rootScope.$on('$routeChangeError', function(event, current, previous) {
                if (previous) {
                    $location.path(previous.originalPath);
                } else {
                    $window.location.reload();
                }
            });
        }]);

})(window, angular, toastr);