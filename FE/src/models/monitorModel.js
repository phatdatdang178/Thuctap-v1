const toJson = (item, listMenu) => {
    return {
      _id: item._id,
      url:item.url,
      serviceId:item.serviceId,
      trangThai:item.trangThai,
      phuongThuc:item.phuongThuc,
      bodyParams:item.bodyParams,
      ghiChu:item.ghiChu,
      code:item.code,
      time:item.time,
      callTimes:item.callTimes,
      name: item.name,
      isActive:item.isActive
    };
  };
  const fromJson = (item) => {
    return {
        _id: item._id,
        url:item.url,
        serviceId:item.serviceId,
        trangThai:item.trangThai,
        phuongThuc:item.phuongThuc,
        bodyParams:item.bodyParams,
        ghiChu:item.ghiChu,
        code:item.code,
        time:item.time,
        callTimes:item.callTimes,
        name: item.name,
        isActive:item.isActive
    };
  };

  const baseJson = () => {
    return {
      _id: null,
      name: null,
      diaChi: null,
      sdt: null,
      content: null,
      link: null,
    };
  };
  const toListModel = (items) => {
    if (items.length > 0) {
      let data = [];
      items.map((value, index) => {
        data.push({
          id: value.id,
          name: value.label != null ? value.label : value.name,
        });
      });
      return data ?? [];
    }
    return [];
  };

  export const doanhNghiepModel = {
    toJson,
    fromJson,
    baseJson,
    toListModel
  };