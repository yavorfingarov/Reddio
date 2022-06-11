BEGIN TRANSACTION;

INSERT INTO KnownDomain (Domain) VALUES ('www.youtube.com');

INSERT INTO KnownDomain (Domain) VALUES ('youtu.be');

INSERT INTO KnownDomain (Domain) VALUES ('youtube.com');

INSERT INTO KnownDomain (Domain) VALUES ('m.youtube.com');

INSERT INTO KnownDomain (Domain) VALUES ('soundcloud.com');

INSERT INTO KnownDomain (Domain) VALUES ('www.mixcloud.com');

INSERT INTO KnownDomain (Domain) VALUES ('vimeo.com');

COMMIT;
