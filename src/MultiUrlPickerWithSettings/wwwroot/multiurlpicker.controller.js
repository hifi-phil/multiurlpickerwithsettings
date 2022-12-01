function multiUrlPickerController($scope, localizationService, entityResource, iconHelper, editorService, overlayService, blockEditorService) {

    var vm = {
        labels: {
            general_recycleBin: ""
        }
    };

    $scope.allowAdd = !$scope.readonly;
    $scope.allowEdit = !$scope.readonly;
    $scope.allowRemove = !$scope.readonly;

    let removeAllEntriesAction = {
        labelKey: "clipboard_labelForRemoveAllEntries",
        labelTokens: [],
        icon: "icon-trash",
        method: removeAllEntries,
        isDisabled: !$scope.allowRemove,
        useLegacyIcon: false
    };

    $scope.renderModel = [];

    if ($scope.preview) {
        return;
    }

    if ($scope.model.config && parseInt($scope.model.config.maxNumber) !== 1 && $scope.umbProperty) {
        var propertyActions = [
          removeAllEntriesAction
        ];

        $scope.umbProperty.setPropertyActions(propertyActions);
    }

    if (!Array.isArray($scope.model.value)) {
        $scope.model.value = [];
    }

    $scope.sortableOptions = {
        axis: "y",
        containment: "parent",
        distance: 10,
        opacity: 0.7,
        tolerance: "pointer",
        scroll: true,
        zIndex: 6000,
        disabled: $scope.readonly,
        update: function () {
            setDirty();
        }
    };

    $scope.model.value.forEach(link => {
        link.icon = iconHelper.convertFromLegacyIcon(link.icon);
        $scope.renderModel.push(link);
    });

    $scope.$on("formSubmitting", () => {
        $scope.model.value = $scope.renderModel;
    });

    $scope.$watch(
        function () {
            return $scope.renderModel.length;
        },
        function () {
            //Validate!
            if ($scope.model.config && $scope.model.config.minNumber && parseInt($scope.model.config.minNumber) > $scope.renderModel.length) {
                $scope.multiUrlPickerForm.minCount.$setValidity("minCount", false);
            }
            else {
                $scope.multiUrlPickerForm.minCount.$setValidity("minCount", true);
            }

            if ($scope.model.config && $scope.model.config.maxNumber && parseInt($scope.model.config.maxNumber) < $scope.renderModel.length) {
                $scope.multiUrlPickerForm.maxCount.$setValidity("maxCount", false);
            }
            else {
                $scope.multiUrlPickerForm.maxCount.$setValidity("maxCount", true);
            }

            $scope.sortableOptions.disabled = $scope.renderModel.length === 1 || $scope.readonly;

            removeAllEntriesAction.isDisabled = $scope.renderModel.length === 0 || $scope.readonly;
            
            //Update value
            $scope.model.value = $scope.renderModel;
        }
    );

    $scope.remove = function ($index) {
        if (!$scope.allowRemove) return;

        $scope.renderModel.splice($index, 1);
        
        setDirty();
    };

    $scope.clear = function ($index) {
      $scope.renderModel = [];

      setDirty();
    };

    $scope.openLinkPicker = function (link, $index) {
        if (!$scope.allowAdd || !$scope.allowEdit) return;

        var target = link ? {
            name: link.name,
            anchor: link.queryString,
            udi: link.udi,
            url: link.url,
            target: link.target
        } : null;

        const linkPicker = {
            currentTarget: target,
            dataTypeKey: $scope.model.dataTypeKey,
            ignoreUserStartNodes : ($scope.model.config && $scope.model.config.ignoreUserStartNodes) ? $scope.model.config.ignoreUserStartNodes : "0",
            hideAnchor: $scope.model.config && $scope.model.config.hideAnchor ? true : false,
            size: $scope.model.config.overlaySize,
            submit: model => {
                if (model.target.url || model.target.anchor) {
                    // if an anchor exists, check that it is appropriately prefixed
                    if (model.target.anchor && model.target.anchor[0] !== '?' && model.target.anchor[0] !== '#') {
                        model.target.anchor = (model.target.anchor.indexOf('=') === -1 ? '#' : '?') + model.target.anchor;
                    }
                    if (link) {
                        link.udi = model.target.udi;
                        link.name = model.target.name || model.target.url || model.target.anchor;
                        link.queryString = model.target.anchor;
                        link.target = model.target.target;
                        link.url = model.target.url;
                    } else {
                        link = {
                            name: model.target.name || model.target.url || model.target.anchor,
                            queryString: model.target.anchor,
                            target: model.target.target,
                            udi: model.target.udi,
                            url: model.target.url
                        };
                        $scope.renderModel.push(link);
                    }

                    if (link.udi) {
                        const entityType = model.target.isMedia ? "Media" : "Document";

                        entityResource.getById(link.udi, entityType).then(data => {

                            link.icon = iconHelper.convertFromLegacyIcon(data.icon);
                            link.published = (data.metaData && data.metaData.IsPublished === false && entityType === "Document") ? false : true;
                            link.trashed = data.trashed;

                            if (link.trashed) {
                                item.url = vm.labels.general_recycleBin;
                            }
                        });
                    } else {
                        link.icon = "icon-link";
                        link.published = true;
                    }

                    setDirty();
                }
                editorService.close();
            },
            close: () => {
                editorService.close();
            }
        };

        editorService.linkPicker(linkPicker);
    };

    function setDirty() {
        if ($scope.multiUrlPickerForm) {
            $scope.multiUrlPickerForm.modelValue.$setDirty();
        }
    }

    function removeAllEntries() {
        localizationService.localizeMany(["content_nestedContentDeleteAllItems", "general_delete"]).then(function (data) {
          overlayService.confirmDelete({
            title: data[1],
            content: data[0],
            close: function () {
              overlayService.close();
            },
            submit: function () {
              $scope.clear();
              overlayService.close();
            }
          });
        });
    }

    function init() {

        localizationService.localizeMany(["general_recycleBin"])
            .then(data => {
                vm.labels.general_recycleBin = data[0];
            });

        // if the property is mandatory, set the minCount config to 1 (unless of course it is set to something already),
        // that way the minCount/maxCount validation handles the mandatory as well
        if ($scope.model.validation && $scope.model.validation.mandatory && !$scope.model.config.minNumber) {
            $scope.model.config.minNumber = 1;
        }

        _.each($scope.model.value, function (item) {
            // we must reload the "document" link URLs to match the current editor culture
            if (item.udi && item.udi.indexOf("/document/") > 0) {
                item.url = null;
                entityResource.getUrlByUdi(item.udi).then(data => {
                    item.url = data;
                });
            }
        });
    }

    init();


    /* This is the setting specific code */


    $scope.allowEditSettings = $scope.model.config.block.length == 1;

    var fakeValue = {};
    var modelObject = blockEditorService.createModelObject(fakeValue, "Umbraco.BlockList", $scope.model.config.block, $scope, $scope);
    modelObject.load();

    var blockObjects = [];

    $scope.openSettings = function (link, $index) {

        var isNew = false;

        if (!blockObjects[$index]) {


            var entry = modelObject.create($scope.model.config.block[0].contentElementTypeKey);
            var blockObject = modelObject.getBlockObject(entry);

            if (link.settings) {
                mapToPropertyModel(blockObject.content, link.settings);
            }
            else {
                isNew = true;
            }

            blockObjects.push(blockObject);
        }

        editBlock(link, $index, isNew);

    }

    function editBlock(link, $index, isNew) {

        var blockObject = blockObjects[$index];

        blockObject.active = true;

        // make a clone to avoid editing model directly.
        var blockContentClone = Utilities.copy(blockObject.content);

        var blockEditorModel = {
            $parentScope: $scope, // pass in a $parentScope, this maintains the scope inheritance in infinite editing
            $parentForm: $scope.propertyForm, // pass in a $parentForm, this maintains the FormController hierarchy with the infinite editing view (if it contains a form)
            hideContent: false,
            openSettings: false,
            createFlow: isNew,
            liveEditing: false,
            title: blockObject.label,
            view: "views/common/infiniteeditors/blockeditor/blockeditor.html",
            size: "medium",
            hideSubmitButton: false,
            submit: function (blockEditorModel) {

                // transfer values when submitting in none-liveediting mode.
                blockObject.retrieveValuesFrom(blockEditorModel.content, blockEditorModel.settings);

                link.settings = modelObject.value.contentData[$index];

                blockObject.active = false;
                editorService.close();
            },
            close: function (blockEditorModel) {
                blockObject.active = false;
                editorService.close();
            }
        };

        blockEditorModel.content = blockContentClone;

        // open property settings editor
        editorService.open(blockEditorModel);
    }

    function mapToPropertyModel(elementModel, dataModel) {

        if (!elementModel || !elementModel.variants || !elementModel.variants.length) { return; }

        var variant = elementModel.variants[0];

        for (var t = 0; t < variant.tabs.length; t++) {
            var tab = variant.tabs[t];

            for (var p = 0; p < tab.properties.length; p++) {
                var prop = tab.properties[p];

                prop.value = dataModel[prop.alias];
            }
        }

    }


    /* End of the setting specific code */

}

angular.module("umbraco").controller("Yuzu.PropertyEditors.MultiUrlPickerController", multiUrlPickerController);
