$(document).ready(function(){
      $('.modal').modal();
      $( "#mpopup" ).hide();

      var months = ['Январь','Февраль','Март','Апрель','Май','Июнь','Июль','Август','Сентябрь','Октябрь','Ноябрь','Декабрь']
      var monshot = ['Янв','Фев','Март','Апр','Май','Июнь','Июль','Авг','Сент','Окт','Нояб','Дек']
      var weekdaysAbbrev =['ВС','ПН','ВТ','СР','ЧТ','ПТ','СБ']
      $('.datepicker').datepicker({"firstDay":1,"format" : "dd.mm.yyyy","i18n" : {"months":months,"monthsShort":monshot,"weekdaysAbbrev":weekdaysAbbrev,"weekdaysShort":weekdaysAbbrev}});
      $('.collapsible').collapsible();
      $('select').formSelect();
      

})